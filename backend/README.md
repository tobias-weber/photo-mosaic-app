# Backend – Photo Mosaic Web App

This is the **ASP.NET Core 9 Web API** that powers the Photo Mosaic application.  
It handles image uploads, mosaic generation, and metadata storage in the backing database.  

---

## 🚀 Running in Development

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download)

### Steps
1. Ensure the containers required for processing are running

2. Navigate to the backend folder:
   ```bash
   cd backend
   ```
3. Restore dependencies:
    ```bash
    dotnet restore
    ```
4. Run the API locally:
    ```bash
    dotnet run
    ```

## 🛠 Notes

Configuration is loaded from appsettings.json and appsettings.Development.json.

For development, uploaded files and the SQLite database are stored in the `storage/` directory.

For production, it is recommended to use the supplied [`docker-compose.yml`](../docker-compose.yml) file in the project root. It automatically creates a volume for persistent file storage 
and PostgreSQL is used as the database (it is recommended to change the default password in [`docker-compose.yml`](../docker-compose.yml)).
Processing containers are deployed as well.
It is also important to change the Admin password and JWT key. This can be done with the environment variables `Admin__Password` and `Jwt__Key`.

---
