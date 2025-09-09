# Backend – Photo Mosaic Web App

This is the **ASP.NET Core 9 Web API** that powers the Photo Mosaic application.  
It handles image uploads, mosaic generation, and metadata storage in SQLite.  

---

## 🚀 Running in Development

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- (Optional) Rider / Visual Studio Code / Visual Studio

### Steps
1. Navigate to the backend folder:
   ```bash
   cd backend
   ```
2. Restore dependencies:
    ```bash
    dotnet restore
    ```
3. Run the API locally:
    ```bash
    dotnet run
    ```

## 🛠 Notes

Configuration is loaded from appsettings.json and appsettings.Development.json.

For development, uploaded files and the SQLite database are stored in the `storage/` directory.

For production, it is recommended to use the supplied [`docker-compose.yml`](../docker-compose.yml) file in the project root. It automatically creates a volume for persistent file and database storage.

---
