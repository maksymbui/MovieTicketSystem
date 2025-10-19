# Movie Tickets – Assignment 2

## File References

- High cohesion/low coupling: `backend/src/MovieTickets.Core/Logic/*`, `Entities/*`, `Models/*`
- Polymorphism: `backend/src/MovieTickets.Core/Logic/BookingService.cs` (method overriding), `Entities/User.cs` (if inheritance used)
- Interfaces: `backend/src/MovieTickets.Core/Logic/IUserRepository.cs`, `MessageService.cs`, `DealService.cs`
- NUnit tests: `backend/src/MovieTickets.Tests/DealServiceTests.cs`
- Anonymous method with LINQ/Lambda: `backend/src/MovieTickets.Core/Logic/RewardService.cs`, `DealService.cs`, `BookingService.cs`
- Generics/Generic Collections: `List<T>` and `Dictionary<K,V>` in `DataStore.cs`, `BookingService.cs`, `PricingService.cs`
- Multiple GUI screens: `frontend/src/pages/SeatSelectionPage.tsx`, `CheckoutPage.tsx`, `DealsPage.tsx`, `LoginPage.tsx`, `RegisterPage.tsx`, `MailBoxPage.tsx`, `MovieDetailsPage.tsx`, `MyOrdersPage.tsx`, `AdminMenuPage.tsx`, `ManageDealsPage.tsx`
- Multiple UI element categories: Buttons, headings, dropdowns, images, lists, modals, alerts, tooltips, etc. (see above pages)
- Core features, error handling, input validation, data structures/algorithms:
  - Backend: `BookingService.cs`, `PricingService.cs`, `RewardService.cs`, `DataStore.cs`, `DealService.cs`, `MessageService.cs`
  - Frontend: All main pages in `frontend/src/pages/`
- Use of ASP.NET Core (not WinForms): `backend/src/MovieTickets.Api/`
- Use of external APIs: TMDB API integration in `backend/src/MovieTickets.Api/Services/TmdbImportService.cs`
- JSON data: `/storage/*.json` (auditoriums, bookings, cinemas, messages, movies, screenings, ticket_types)

## Initial setup:

```bash
Open terminal:
- `nix develop`
- `dotnet run --project backend/src/MovieTickets.Api/MovieTickets.Api.csproj` # Expected: "Now listening on: https://localhost:####"

Open another terminal:
- `nix develop`
- `export TMDB__APIKEY="df0b8bc6934d37266ef32754dfa21420"`
- `curl -X POST http://localhost:####/api/admin/import/tmdb` # Note: Change localhost port number with whatever is set.

- `dotnet run --project tools/SeedGenerator`# Expected: [seed] Wrote # cinemas, # auditoriums, # screenings, # bookings
```

## Run:

```bash
- `chmod +x start.sh`
- `./start.sh`
```

> [!NOTE]
> To refresh the movie catalog from TMDB, set `TMDB__APIKEY` (or edit `backend/src/MovieTickets.Api/appsettings.json`) and call `POST http://localhost:####/api/admin/import/tmdb`... and rerun: `dotnet run --project tools/SeedGenerator`.

File structure is relatively self-explanatory.

- `/backend`: Stores logic and seeded data.
- `/desktop`: Electron, nuff said.
- `/frontend`: Honestly, I have no idea, shit is straight up vibe-coded, I don't do UI, I know enough to theme my dotfiles but making a website? couldn't care less. Make sure to check Blame though.
- `/storage`: Theoretically, this directory isn't needed, but I'm not bothered enough to refactor it. Tldr, its just filler data. We would curl first, then use SeedGenerator, as the CopyToOutputDirectory is set as PreserveNewest, out data won't overrite itself.

> [!WARNING]
> After navigating towards the seat selection screen, the 'Quick Book' option is there for demo/testing, it auto-fills your current selection with adults and calls the same booking endpoint. It is not indicative of actual performance (nor is the rest of data)

## For Windows:

WSL with Ubuntu: (doesnt matter, nix is distro-agnostic)

```bash
WSL:
- `wsl --install -d Ubuntu-24.04` # Onetime, just for installing
- `wsl --shutdown` # Actually shutdown
- `wsl.exe -d Ubuntu-24.04` # Open

- `sudo apt update && sudo apt upgrade -y`

- `sh <(curl -L https://nixos.org/nix/install) --no-daemon . ~/.nix-profile/etc/profile.d/nix.sh` # installs nix
OR
- `sh <(curl -L https://nixos.org/nix/install) --daemon` # Should work...

Add to group:
- `sudo usermod -aG nix-users $USER` # this should be the main one
- `sudo usermod -aG nixbld $USER` # but I also have this just in case.... adds ur user group, make sure to run `wsl --shutdown` in Powershell (admin) and rerun with `wsl.exe -d Ubuntu-24.04` or whatever you use.

IF, groups are still missing (run: `grep nixbld /etc/group` and check, or just `group`):
- `sudo groupadd nixbld`
- `sudo usermod -aG nixbld $USER`

Enabling flakes:
- `mkdir -p ~/.config/nix`
- `echo "experimental-features = nix-command flakes" >> ~/.config/nix/nix.conf`

Checks:
- `nix --version` # verify it even exists
- `sudo systemctl start nix-daemon` # just in case its sleeping
- `sudo ls -ld /nix/var/nix /nix/var/nix/daemon-socket /nix/var/nix/daemon-socket/socket` # final check, for if you have perms to run nix commands or not
```
