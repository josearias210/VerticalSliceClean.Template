# 🚨 Important: First Steps After Creating Project from Template

## 1. Create Initial Migration

The template does NOT include pre-made migrations. You must create them after generating your project.

### Why?
- Migrations reference specific assembly names
- Each project needs its own migration history
- Allows you to start fresh with your domain entities

### Steps:

```powershell
# 1. Navigate to Infrastructure project
cd src/[YourProject].Infrastructure

# 2. Create initial migration
dotnet ef migrations add InitialCreate --startup-project ../[YourProject].AppHost

# 3. Verify migration was created
dir Persistence\EF\Migrations
```

## 2. Run the Application

The application will automatically:
- Create the database (if it doesn't exist)
- Apply all migrations
- Seed roles and admin user

```powershell
cd src/[YourProject].AppHost
dotnet run
```

## 3. Verify Database

Check that tables were created:
- `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.
- `RefreshTokens`

## 4. Add Your Domain Entities

Now you're ready to add your own entities!

See [MIGRATIONS.md](MIGRATIONS.md) for complete guide on:
- Adding entities
- Creating migrations
- Production deployment strategies
- Troubleshooting

---

**Quick Reference:**
- 📖 [Full Migrations Guide](MIGRATIONS.md)
- 🏗️ [Architecture Decisions](adr/)
- 🔒 [Security Documentation](../SECURITY.md)
