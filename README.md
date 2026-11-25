# AQUILLA TEXTILES

Full-stack textile website using HTML, CSS, JavaScript, ASP.NET Core 10 (C#), PostgreSQL, and JWT.

## Included
- Login and account creation
- BCrypt password hashing
- JWT authentication (2-hour access token)
- Forgot/reset password API flow
- Single-page collections view: Men's, Women's, Kids'
- Responsive layout
- ASP.NET serves the frontend and API from the same application, so one Render service can provide one public URL.

## Requirements
- .NET 10 SDK
- VS Code (C# Dev Kit recommended)
- PostgreSQL database (Aiven for deployment)

## Run locally in VS Code

1. Open the `AQUILLA-TEXTILES` folder in VS Code.
2. Edit `appsettings.json` and set your local PostgreSQL connection string.
3. Open Terminal in the project folder and run:

```bash
dotnet restore
dotnet run
```

4. Open the local URL printed in the terminal (usually `http://localhost:5000` or `https://localhost:7xxx`).

The first run creates the database table automatically using `EnsureCreated`. For a long-lived production system, replace this with EF Core migrations before making schema changes.

## Aiven PostgreSQL setup

1. Create a PostgreSQL service in Aiven.
2. Open the service's connection information and copy the host, port, database, username, and password.
3. In Aiven's connection details, download/use the CA certificate if required by your service configuration.
4. Set Render's `DATABASE_URL` to an Npgsql connection string, for example:

```text
Host=YOUR_AIVEN_HOST;Port=YOUR_PORT;Database=defaultdb;Username=avnadmin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

Do not commit real credentials to GitHub. Use Render environment variables.

## Render deployment (single website URL)

1. Push this complete folder to a GitHub repository.
2. In Render, choose **New → Web Service**, connect the repository.
3. Select **Docker** as the runtime (Render will use the included Dockerfile).
4. Add these environment variables in Render:

| Key | Value |
|---|---|
| `DATABASE_URL` | Your Aiven Npgsql connection string |
| `JWT_SECRET` | A random secret at least 32 characters long |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `JWT_ISSUER` | `AquillaTextiles` |
| `JWT_AUDIENCE` | `AquillaTextilesUsers` |

5. Deploy. Render gives the service one URL; that URL serves both frontend and backend.
6. Test `https://YOUR-RENDER-URL/api/health`.

Generate a JWT secret locally with:

```bash
openssl rand -base64 48
```

## Important password recovery note

The forgot-password endpoint creates a short-lived reset token. In Development it returns the token so you can test the flow. In Production it intentionally does not expose the token. Before real customers use password recovery, connect an email provider (SMTP or transactional email service) and send a secure reset link to a verified email address. The current signup form only asks for username and password, so a production recovery system should also collect and verify an email address.

## Security notes
- Passwords are hashed with BCrypt and are never stored as plain text.
- JWT secret and database credentials must be configured as environment variables in production.
- Add rate limiting, HTTPS-only deployment, email verification, logging/monitoring, and database backups before public production use.
- The collection cards are selectable options on the same page; they do not navigate to another page.


## Email registration and password recovery

The signup form now requires a unique email address. Forgot Password accepts the registered email address, sends a six-digit code, and allows the user to set a new password. Codes expire after 10 minutes and are stored as SHA-256 hashes.

### Update an existing Aiven database

Run `database-email-update.sql` once in the Aiven SQL editor. This adds nullable email columns and a unique index. Existing accounts remain intact; they do not have an email address until you update them.

### Configure SMTP (required for real email delivery)

Set these environment variables in the environment where the app runs. Do not commit SMTP credentials to Git or share them in chat:

- `EMAIL_SMTP_HOST` — SMTP server hostname
- `EMAIL_SMTP_PORT` — usually `587`
- `EMAIL_SMTP_USER` — SMTP login / sender account
- `EMAIL_SMTP_PASSWORD` — SMTP password or provider app password
- `EMAIL_FROM` — verified sender email address

For local PowerShell, set them in the same terminal before `dotnet run`, for example (replace values locally):

```powershell
$env:EMAIL_SMTP_HOST = "smtp.example.com"
$env:EMAIL_SMTP_PORT = "587"
$env:EMAIL_SMTP_USER = "your-sender@example.com"
$env:EMAIL_SMTP_PASSWORD = "YOUR_SMTP_APP_PASSWORD"
$env:EMAIL_FROM = "your-sender@example.com"
dotnet run
```

For Render, add the same variables under the service's Environment settings. Use an SMTP provider account that permits application sending. Until SMTP is configured, password recovery returns a configuration error; it will not pretend that a code was emailed.


## Heritage visual design update

The login, sign-up, and password-recovery screens use the supplied AQUILLA TEXTILES heritage reference:
- Desktop: split layout with the supplied maroon-and-gold AQ/silk-saree artwork on the left and warm ivory form panel on the right.
- Mobile: the same artwork becomes a full-width top banner, with the form beneath it.
- Colors, serif headings, gold ornamental dividers, rounded input fields, and maroon/gold action buttons follow the supplied reference.
- The artwork is included locally at `wwwroot/images/aquilla-heritage-panel.jpg`, so the branding panel does not depend on an external image host.

The supplied artwork is a raster image used as the branding panel. Form controls remain real HTML inputs and buttons.

## Local verification

The project files have been updated, but build/runtime verification must be performed in your VS Code environment with the .NET 10 SDK installed. Keep your existing Aiven connection string and JWT secret in environment variables; do not replace them with sample credentials.
