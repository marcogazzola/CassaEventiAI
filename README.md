# CassaEventiAI

Applicazione desktop WPF (`net10.0-windows`) per la gestione cassa eventi: vendita, stampa scontrini, storno/riattivazione, reportistica, configurazione reparti/articoli/operatori e backup.

## Requisiti

- Windows 10/11
- .NET 10 SDK
- Stampante Windows installata (se si usa la stampa)

## Build ed esecuzione

```bash
cd src
dotnet restore
dotnet build
dotnet run
```

## Publish

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Stack tecnico

- UI: WPF + MVVM (`CommunityToolkit.Mvvm`)
- Persistenza: SQLite (`Microsoft.EntityFrameworkCore.Sqlite`)
- DI: `Microsoft.Extensions.DependencyInjection`
- Export Excel: `ClosedXML`
- Sicurezza password: `BCrypt.Net-Next`
- Target framework: `net10.0-windows`

## Struttura progetto

```text
CassaEventiAI/
├── CassaEventiAI.sln
└── src/
    ├── App.xaml / App.xaml.cs
    ├── CassaEventiAI.csproj
    ├── Data/
    │   └── CassaDbContext.cs
    ├── Models/
    │   └── Models.cs
    ├── Services/
    │   ├── AuthService.cs
    │   ├── BackupService.cs
    │   ├── ConfigService.cs
    │   ├── EventService.cs
    │   ├── ProductService.cs
    │   ├── ReceiptService.cs
    │   ├── ReportService.cs
    │   ├── SaleService.cs
    │   └── UsbService.cs
    ├── Printing/
    │   └── PrintingService.cs
    ├── ViewModels/
    │   ├── BaseViewModel.cs
    │   ├── FrontOfficeViewModel.cs
    │   ├── BackOfficeViewModel.cs
    │   ├── ReportViewModel.cs
    │   ├── StartupViewModel.cs
    │   ├── LoginViewModel.cs
    │   ├── DepartmentsViewModel.cs
    │   ├── ProductsViewModel.cs
    │   └── OperatorsViewModel.cs
    ├── Views/
    │   ├── FrontOffice/MainWindow.xaml
    │   ├── BackOffice/BackOfficeWindow.xaml
    │   ├── Reports/ReportWindow.xaml
    │   └── Shared/
    │       ├── StartupWindow.xaml
    │       ├── LoginWindow.xaml
    │       └── ReceiptPreviewWindow.xaml
    ├── Converters/Converters.cs
    └── Resources/Themes/LightTheme.xaml
```

## Funzionalità funzionali

### FrontOffice (cassa)

- Articoli raggruppati per reparto (layout multi-colonna).
- Evidenza stock residuo su articolo quando `TrackStock=true`.
- Carrello con aumento/riduzione quantità.
- Vincolo stock: se abilitato non è possibile vendere oltre disponibilità.
- Decremento stock in vendita e ripristino stock su storno.
- Sconto percentuale con range `0..100`.
- Metodi di pagamento multipli (layout 3 per riga).
- Calcolo resto automatico per pagamenti cash.
- Anteprima scontrino coerente con stampa.
- Ristampa ultimo scontrino.
- Storno con ricerca scontrini emessi e riattivazione scontrini stornati.
- Header con nome utente connesso e orologio live.
- Footer con metriche sessione e toolbar operativa.

### BackOffice (Settings)

- Gestione reparti:
  - attivazione/disattivazione
  - ordine visualizzazione
  - flag `PrintSeparateReceipt`
  - colore reparto (gli articoli ereditano il colore del reparto)
- Gestione articoli:
  - prezzo
  - reparto
  - stato attivo
  - stock (`TrackStock`, `StockQty`)
- Gestione operatori:
  - username/display name
  - ruolo (`admin` / `cashier`)
  - stato attivo
- Gestione metodi di pagamento.
- Configurazione scontrino (header/footer e opzioni stampa).
- Selezione stampante Windows installata (nessuna porta seriale).
- Tema scuro rimosso: tema unico chiaro.

### Reportistica (finestra dedicata)

- Selezione periodo `Da` / `A`.
- Tab **Incasso alla data/periodo**:
  - numero scontrini emessi
  - numero scontrini annullati
  - elenco prodotti con quantità e importo totale
  - totale venduto
- Tab **Ordini emessi alla data/periodo**:
  - elenco ordini con dettaglio ordine selezionato
  - anteprima scontrino dell’ordine
  - layout colonne 60% elenco / 40% dettaglio
- Export Excel con periodo selezionato.

## Stampa scontrini (tecnica + formato)

- Stampa e anteprima generate dalla stessa pipeline (`ReceiptService` + `PrintingService`).
- Scontrino testuale unico con separazione sezioni tramite carattere di taglio carta.
- Sezione fiscale con:
  - intestazione da configurazione
  - numero ordine, operatore, data/ora
  - righe prodotto con quantità/prezzo/totale
  - sconto
  - totale, pagato e resto (per cash)
  - piè di pagina da configurazione
- Sezioni reparto (ritiro) create solo per reparti con `PrintSeparateReceipt=true`.
- Quantità senza decimali, importi con `€`.

## Ruoli, autorizzazioni e visibilità

| Funzionalità | Admin | Cashier |
|---|---:|---:|
| Accesso Settings/BackOffice | Sì | No |
| Accesso Reportistica | Sì | No |
| Totali footer | Sempre visibili | Visibili solo se `ShowTotalInFooter=true` |
| Operazioni cassa (vendita/stampa/storno) | Sì | Sì |

## Sicurezza

- Login operatori con password hashate tramite BCrypt.
- Supporto ruoli applicativi (`admin`, `cashier`).
- Blocco eliminazione dell’utente correntemente loggato.
- Vincolo almeno un amministratore attivo.
- Supporto flusso cambio password obbligatorio (`MustChangePassword`).
- Validazioni input numerici in UI (sconto, contanti, ecc.).
- Gestione errori con messaggistica utente nei flussi critici.

## Persistenza dati

### Configurazioni JSON

Posizione: `%APPDATA%\CassaEventiAI\Data\`

| File | Contenuto |
|---|---|
| `app_settings.json` | Impostazioni applicative (evento attivo, stampante, backup, footer totals, ecc.) |
| `departments.json` | Reparti (`PrintSeparateReceipt`, colore, ordine, attivo) |
| `products.json` | Articoli (prezzo, reparto, stock) |
| `payment_methods.json` | Metodi di pagamento |
| `operators.json` | Operatori e ruoli |
| `receipt_config.json` | Configurazione intestazione/piè di pagina e opzioni scontrino |

### Database SQLite evento

Posizione: `%APPDATA%\CassaEventiAI\Events\<nome_evento>.db`

Archivi eventi: `%APPDATA%\CassaEventiAI\Archive\`

Tabelle principali:

- `Sales`
- `SaleItems`
- `OperatorShifts`

## Flusso operativo sintetico

1. Selezione/creazione evento.
2. Login operatore.
3. Apertura/aggancio turno operatore.
4. Vendita con stampa/anteprima.
5. Eventuale storno o riattivazione.
6. Chiusura turno con riepilogo.
7. Reportistica e/o export Excel.
8. Backup automatici o manuali.

## Pacchetti NuGet (versioni correnti)

| Package | Versione |
|---|---|
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.9 |
| Microsoft.EntityFrameworkCore.Design | 10.0.9 |
| CommunityToolkit.Mvvm | 8.4.2 |
| Microsoft.Extensions.DependencyInjection | 10.0.9 |
| Microsoft.Extensions.Logging | 10.0.9 |
| ClosedXML | 0.104.2 |
| BCrypt.Net-Next | 4.2.1 |
| CsvHelper | 33.1.0 |
| SixLabors.ImageSharp | 3.1.12 |
| ESCPOS_NET | 3.0.0 |

## Note operative

- Se `CassaEventiAI.exe` è aperto, la build può fallire per file lock: chiudere il processo prima di compilare.
- Le funzionalità di reportistica e settings sono intenzionalmente riservate agli admin.
