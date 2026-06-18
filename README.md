# CPL Cassa Eventi

Applicazione Windows WPF (.NET 10) per la gestione della cassa agli eventi dell'associazione CPL Concordia.

## Requisiti

- Windows 10/11
- .NET 10 SDK (https://dotnet.microsoft.com/download)
- Visual Studio 2022 v17.12+ oppure VS Code con estensione C#
- Stampante Windows configurata nel sistema (opzionale — la cassa funziona anche senza)

## Struttura progetto

```
CplCassaEventi/
├── CplCassaEventi.sln
└── src/
    ├── App.xaml / App.xaml.cs          ← DI container, startup, tema
    ├── Models/
    │   └── Models.cs                   ← Tutte le entity EF Core + config model
    ├── Data/
    │   └── CassaDbContext.cs           ← EF Core DbContext (SQLite per-evento)
    ├── Services/
    │   ├── AuthService.cs              ← Login operatori (BCrypt)
    │   ├── BackupService.cs            ← Backup automatico e su USB
    │   ├── ConfigService.cs            ← Lettura/scrittura file JSON config
    │   ├── EventService.cs             ← Ciclo vita evento (crea/apri/archivia)
    │   ├── ProductService.cs           ← Gestione prodotti e reparti
    │   ├── ReceiptService.cs           ← Composizione testo scontrino
    │   ├── ReportService.cs            ← Query report e export CSV
    │   ├── SaleService.cs              ← Vendite, storno, turni, contatore persone
    │   └── UsbService.cs               ← Rilevamento chiavette USB
    ├── Printing/
    │   └── PrintingService.cs          ← ESC/POS via ESCPOS_NET
    ├── ViewModels/
    │   ├── BaseViewModel.cs
    │   ├── BackOfficeViewModel.cs
    │   ├── FrontOfficeViewModel.cs
    │   ├── LoginViewModel.cs
    │   ├── ReportViewModel.cs
    │   └── StartupViewModel.cs
    ├── Views/
    │   ├── FrontOffice/
    │   │   ├── MainWindow.xaml         ← Schermata cassa principale
    │   │   └── MainWindow.xaml.cs
    │   ├── BackOffice/
    │   │   ├── BackOfficeWindow.xaml   ← Gestione evento, config, backup
    │   │   └── BackOfficeWindow.xaml.cs
    │   └── Shared/
    │       ├── StartupWindow.xaml      ← Wizard primo avvio / selezione evento
    │       ├── LoginWindow.xaml        ← Login operatore
    │       └── *.xaml.cs
    ├── Converters/
    │   └── Converters.cs              ← BoolToVis, Euro, NullToCollapsed
    └── Resources/
        └── Themes/
            ├── LightTheme.xaml
            └── DarkTheme.xaml
```

## File di configurazione (JSON — condivisi tra eventi)

Posizione: `%APPDATA%\CplCassaEventi\Data\`

| File | Contenuto |
|------|-----------|
| `departments.json` | Reparti (id, nome, colore, ordine) |
| `products.json` | Articoli (id, nome, prezzo, reparto, stock) |
| `payment_methods.json` | Metodi di pagamento configurabili |
| `operators.json` | Operatori con hash BCrypt |
| `receipt_config.json` | Testo header/footer scontrino |

## Database SQLite (per evento)

Posizione: `%APPDATA%\CplCassaEventi\Events\<nome_evento>.db`

Archiviati in: `%APPDATA%\CplCassaEventi\Archive\<nome_evento>_<data>.db`

Tabelle: `Sales`, `SaleItems`, `OperatorShifts`, `PersonCounters`

## Primo avvio

1. All'avvio viene mostrata la schermata di benvenuto
2. Inserire il nome dell'evento → viene creato il file `.db`
3. Login con credenziali default: `admin` / `admin` (**cambiare subito la password!**)

## Build

```bash
cd src
dotnet restore
dotnet build
dotnet run
```

## Publish standalone

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## NuGet packages

| Package | Versione | Uso |
|---------|----------|-----|
| Microsoft.EntityFrameworkCore.Sqlite | 9.x | ORM database per evento |
| CommunityToolkit.Mvvm | 8.x | MVVM source generators |
| ESCPOS_NET | 3.x | Stampa termica ESC/POS |
| Microsoft.Extensions.DependencyInjection | 9.x | DI container |
| CsvHelper | 33.x | Export report CSV |
| BCrypt.Net-Next | 4.x | Hash password operatori |

## Funzionalità

### FrontOffice (cassa)
- Griglia prodotti touch-friendly filtrata per reparto
- Carrello con modifica quantità +/−
- Sconto percentuale sul totale
- Selezione metodo di pagamento (configurabile in backoffice)
- Inserimento contanti e calcolo resto automatico
- Incassa e stampa scontrino
- Ristampa ultimo scontrino
- Storno scontrino con motivo
- Contatore persone servite
- Footer con: scontrini emessi, totale incassato (opzionale), ultimo ordine, persone

### BackOffice
- Configurazione reparti e listino (salvati in JSON)
- Gestione metodi di pagamento (aggiungi/rimuovi/attiva)
- Configurazione scontrino (header, footer, stampa prezzi, subtotali reparto)
- Impostazioni: stampante configurata, modalità kiosk
- Creazione nuovo evento
- Chiusura e storicizzazione evento (rinomina db con data)
- Archivio eventi con riapertura
- Backup manuale e automatico (configurabile ogni N minuti)
- Backup su chiavetta USB
- Export/Import configurazione da USB
- Reportistica in finestra dedicata con incasso giornaliero, ordini giornalieri, export Excel e anteprima scontrino

### Sicurezza
- Login operatori con password hashata (BCrypt)
- Ruoli: `cashier` (cassa) e `admin` (backoffice completo)
- Chiusura turno operatore con riepilogo
