FROM python:3.12-slim

WORKDIR /app

# Install the libvips library and its dependencies.
# We use apt-get for a stable, robust installation.
# The libvips-dev package provides the necessary headers and libraries to build pyvips.
# --no-install-recommends helps keep the image size minimal.
RUN apt-get update && apt-get install -y libvips-dev build-essential --no-install-recommends

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Clean up build-essential and other dependencies to reduce image size.
# This is an important step for production-ready Docker images.
RUN apt-get purge -y build-essential && apt-get autoremove -y && rm -rf /var/lib/apt/lists/*

COPY . .

CMD ["uvicorn", "api:app", "--host", "0.0.0.0", "--port", "8000"]
