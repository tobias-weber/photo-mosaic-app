# Stage 1: Build the Angular application
FROM node:22-alpine AS build

# Set the working directory
WORKDIR /app

# Copy package.json and package-lock.json to install dependencies
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy the rest of the application source code
COPY . .

# Build the Angular application for production
RUN npm run build --prod

# Stage 2: Serve the application with Nginx
FROM nginx:alpine AS production

# Copy the built application from the 'build' stage
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

# Expose port 80 to the host machine
EXPOSE 80

# The default Nginx CMD serves the static content
CMD ["nginx", "-g", "daemon off;"]
