# Database (MySQL + MySQL Workbench)

Two tables are used:

| Table | Written by | Read by |
| --- | --- | --- |
| `Users` | `schema.sql` (seed) / your SQL | the login flow |
| `ContactMessages` | the portfolio contact form | you, via Workbench |

`schema.sql` creates both, seeds a demo account, and creates the
least-privilege application user.

## 1. Create the schema

1. Open **MySQL Workbench** and connect to your server (usually `localhost:3306`).
2. **File ▸ Open SQL Script…** and select `Database/schema.sql`.
3. Run it with the lightning-bolt button (<kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Enter</kbd>).
4. The final `SELECT` should return one row: `admin@example.com`.

The script is safe to re-run — it uses `CREATE … IF NOT EXISTS` and only inserts
the demo row when it is missing.

## 2. Point the app at it

Edit `ConnectionStrings:Default` in `appsettings.json` (or `appsettings.Development.json`):

```json
"ConnectionStrings": {
  "Default": "Server=localhost;Port=3306;Database=blazor_portfolio;User=blazorapp;Password=CHANGE_ME;SslMode=Preferred"
}
```

Set `Database:ServerVersion` to your server's version (`8.0.36` works for any 8.0
server; use `10.11.0` style values for MariaDB). It is pinned rather than
auto-detected so the app starts — and shows a readable error on the login page —
even when MySQL is not running.

For local work prefer user-secrets over committing a password:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=blazor_portfolio;User=blazorapp;Password=CHANGE_ME;SslMode=Preferred"
```

## 3. Demo credentials

| Email               | Password    |
| ------------------- | ----------- |
| `admin@example.com` | `Passw0rd!` |

**Change or delete this account before deploying anywhere real.**

## 4. Adding users

Passwords are stored as PBKDF2-HMAC-SHA256 in the format
`iterations.base64(salt).base64(key)`. Generate a hash with the built-in helper:

```powershell
dotnet run -- --hash-password "YourNewPassword"
```

Copy the output into an `INSERT` (Workbench, or any MySQL client):

```sql
INSERT INTO `Users` (`Email`, `DisplayName`, `PasswordHash`, `IsActive`, `CreatedUtc`)
VALUES ('jane@example.com', 'Jane Doe', '<paste the hash here>', 1, UTC_TIMESTAMP(6));
```

Store emails lower-cased — the app normalises input before lookup, and the
`IX_Users_Email` unique index is what makes the lookup cheap.

To revoke access without deleting history, set `IsActive = 0`; the user will be
told the account is disabled.

## 5. Contact form messages

Every submission from the portfolio page is **emailed to you and** written to
`ContactMessages`. The email is the primary channel; the row is the fallback so a
mail outage never loses a lead.

### Email setup (required to receive notifications)

The form sends through SMTP, configured in the `Email` section. `Host`,
`FromAddress` and `ToAddress` are already pointed at
`francislagmay24@gmail.com`; only the password is missing, and it must not be
committed. For Gmail:

1. Enable 2-Step Verification, then create an **App password**
   (Google Account ▸ Security ▸ App passwords).
2. Store it in user-secrets:

   ```powershell
   dotnet user-secrets set "Email:Password" "<16-character app password>"
   ```

3. Verify with a submission from `/#contact`. The log should show
   `Emailed contact message from …`.

Other settings in the `Email` section:

| Key | Purpose |
| --- | --- |
| `Security` | `StartTls` (port 587) or `SslOnConnect` (port 465). `None` is for local sinks only. |
| `FromName` | Display name on the notification. |
| `SendAutoReply` | Set `false` to stop the "thanks, message received" mail to the visitor. |
| `CcAddress` | Optional second inbox for a copy. |
| `Enabled` | Set `false` to turn email off and go back to database-only. |

Without a password (or with `Enabled: false`) submissions are still stored and the
form still succeeds — the mail attempt is logged, and the visitor sees the
"received but not emailed" notice.

### Reading stored messages

```sql
SELECT `Id`, `Name`, `Email`, `Company`, `Message`, `CreatedUtc`
FROM `ContactMessages`
WHERE `IsHandled` = 0
ORDER BY `CreatedUtc` DESC;
```

Mark one as dealt with when you have replied:

```sql
UPDATE `ContactMessages` SET `IsHandled` = 1 WHERE `Id` = 7;
```

Behaviours worth knowing:

- **Honeypot.** The form has a hidden `Website` field. If a bot fills it in, the
  message is silently discarded — not stored, not emailed — and the bot is shown
  the normal success message.
- **Partial failures are surfaced.** If the email goes out but MySQL is down, the
  visitor is told the message was received but the notification could not be
  sent. If neither channel works, they are asked to email you directly instead of
  seeing a fake success.
- **Reply-to is the visitor.** Pressing reply in the notification answers them,
  not your own inbox.

## Notes

- No migrations are used. The tables are created by `schema.sql`, and the EF Core
  model in `Data/AppDbContext.cs` is kept in sync with it by hand — update both
  together if you change the shape.
- The `blazorapp` database user gets only `SELECT, INSERT, UPDATE, DELETE`. If you
  later add migrations via `dotnet ef`, that user also needs DDL rights.
- Portfolio copy is not stored in the database. It lives in `portfolio.json` at
  the project root, bound to `Options/PortfolioOptions.cs`.