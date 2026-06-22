using CassaEventiAI.Models;

namespace CassaEventiAI.Services;

public class AuthService(ConfigService config)
{
    public Operator? CurrentOperator { get; private set; }
    public bool IsLoggedIn => CurrentOperator != null;
    public bool IsAdmin => CurrentOperator?.Role == "admin";

    /// <summary>
    /// Returns null on success, or an error message string.
    /// Sets MustChangePassword flag on the session operator.
    /// </summary>
    public string? Login(string username, string password)
    {
        var operators = config.LoadOperators();
        var op = operators.FirstOrDefault(o =>
            o.IsActive &&
            o.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        if (op == null) return "Credenziali non valide.";
        if (!BCrypt.Net.BCrypt.Verify(password, op.PasswordHash)) return "Credenziali non valide.";

        CurrentOperator = op;
        return null;
    }

    public void Logout() => CurrentOperator = null;

    /// <summary>Called at first-run DB creation to seed the admin operator.</summary>
    public Operator CreateFirstAdmin(string username, string displayName, string password)
    {
        var op = new Operator
        {
            Id = config.NextOperatorId(),
            Username = username,
            DisplayName = displayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "admin",
            IsActive = true,
            MustChangePassword = false
        };
        var list = config.LoadOperators();
        list.Add(op);
        config.SaveOperators(list);
        return op;
    }

    public void ChangePassword(int operatorId, string newPassword)
    {
        var ops = config.LoadOperators();
        var op = ops.First(o => o.Id == operatorId);
        op.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        op.MustChangePassword = false;
        config.SaveOperators(ops);

        // Update in-session copy
        if (CurrentOperator?.Id == operatorId)
            CurrentOperator.MustChangePassword = false;
    }

    public void SaveOperator(Operator op)
    {
        var ops = config.LoadOperators();
        var idx = ops.FindIndex(o => o.Id == op.Id);
        if (idx >= 0) ops[idx] = op;
        else { op.Id = config.NextOperatorId(); ops.Add(op); }
        config.SaveOperators(ops);
    }

    public void DeleteOperator(int id)
    {
        if (CurrentOperator?.Id == id)
            throw new InvalidOperationException("Non puoi eliminare l'utente con cui sei connesso.");
        var ops = config.LoadOperators();
        if (ops.Count(o => o.IsActive && o.Role == "admin") <= 1 && ops.Any(o => o.Id == id && o.Role == "admin"))
            throw new InvalidOperationException("Deve esistere almeno un amministratore attivo.");
        ops.RemoveAll(o => o.Id == id);
        config.SaveOperators(ops);
    }

    public List<Operator> GetAllOperators() => config.LoadOperators();
}
