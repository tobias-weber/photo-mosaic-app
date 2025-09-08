# Photo Mosaic Web App

This project demonstrates a **photo mosaic generator** built with:

- **Backend** (`/backend`) — ASP.NET Core 9 Web API  
  Handles user requests, image uploads, mosaic generation logic, and stores metadata in SQLite + raw files on disk (`/data`).  

- **Frontend** (`/frontend`) — Angular + Nginx  
  Provides the UI for uploading images, managing tasks, and displaying generated mosaics.  
  Served via Nginx, which also proxies `/backend-api/` requests to the backend container.  

- **Data** (`/data`) — Volume mount for uploaded images and SQLite database.  
  (This folder is ignored by Git.)

---

## 🚀 Running with Docker Compose

### Prerequisites
- [Docker](https://docs.docker.com/get-docker/)  
- [Docker Compose](https://docs.docker.com/compose/)  

### Steps
1. Clone the repo:
   ```bash
   git clone https://github.com/your-username/photo-mosaic.git
   cd photo-mosaic
   ```
2. Build and start containers:
    ```bash
    docker-compose up --build
    ```
3. Open frontend in your browser with `http://localhost:8080`.


## 🛠 Development Notes
Consult the READMEs for the [frontend](frontend/README.md) or [backend](backend/README.md) for how to run them outside of docker for development.

- Angular dev server runs on `http://localhost:4200` (outside Docker).
- ASP.NET Core API runs on `http://localhost:5243`.
- Angular dev server proxies `/backend-api/` → `http://localhost:5243/`
    - Inside Docker, Nginx proxies `/backend-api/` → `backend:80/api/`.

---
