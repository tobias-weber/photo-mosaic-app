# Photo Mosaic Web App

This project demonstrates a **photo mosaic generator** built with:

- **Backend** (`/backend`) — ASP.NET Core 9 Web API  
  Handles user requests, image uploads, mosaic generation orchestration, and stores metadata in SQLite + raw files on disk (`/backend/storage` during development).  

- **Frontend** (`/frontend`) — Angular  
  Provides the UI for uploading images, managing generation jobs, and displaying generated mosaics.  
  Served via Nginx, which also proxies `/backend-api/` requests to the backend container.

- **Processing** (`/processing`) — Python  
  Computes the actual mosaics by relying a scalable number of worker containers (using the job queue package RQ).
  The backend enqueues a computation by sending a REST request to the python-api container.
  After processing is finished, a REST request is sent from the worker to the backend.  

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
2. Optional: 
      Change the private key used for JWT and default admin password (`adminPw`) to secure new values. This can be done by with environment variables in `docker-compose.yml`:
    ```yml
    services:
      backend:
        environment:
          - Jwt__Key=MyVerySecureNewKey
          - Admin__Password=MyVerySecurePassword
    ```
3. Build and start containers:
    ```bash
    docker-compose up --build
    ```
4. Open frontend in your browser with `http://localhost:8080`.

### Full Reset
To reset the app (deleting the database and images) it is easiest to use the following command:
```bash
docker-compose down -v
```

### Scaling Workers
To enable concurrent mosaic processing, simply run docker compose with multiple workers:
```bash
docker compose up --scale worker=3
```


## 🛠 Development Notes
Consult the READMEs for the [frontend](frontend/README.md) or [backend](backend/README.md) for how to run them outside of docker for development.
For the [processing service](processing/README.md) it is recommended to always rely on docker. To only start the processing containers, run:
```bash
docker compose -f docker-compose.dev.yml up --build
```

Other notes:
- Angular dev server runs on `http://localhost:4200` (outside Docker).
- ASP.NET Core API runs on `http://localhost:5243`.
- Angular dev server proxies `/backend-api/` → `http://localhost:5243/`
  - Inside Docker, Nginx proxies `/backend-api/` → `backend:80/api/`
- Processing Python API runs on `http://localhost:8000`

---

## Third-Party Libraries / Acknowledgements
This project is MIT licensed. In particular, it uses the following third-party libraries:
- libvips – LGPL-2.1. https://github.com/libvips/libvips

- OpenSeadragon – BSD-3 license. https://openseadragon.github.io/

- ImageSharp – Apache 2.0 license. https://github.com/SixLabors/ImageSharp

- SciPy - BSD-3 license. https://github.com/scipy/scipy