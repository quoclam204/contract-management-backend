# ==============================================================================
# 1. Build Stage
# ==============================================================================
FROM node:22-alpine AS build
WORKDIR /app

# Copy dependency manifests
COPY package.json package-lock.json ./

# Clean install dependencies
RUN npm ci

# Copy all source files
COPY . .

# Build the production React bundle
RUN npm run build

# ==============================================================================
# 2. Production Runtime Stage (Nginx)
# ==============================================================================
FROM nginx:alpine AS runtime

# Remove default static files
RUN rm -rf /usr/share/nginx/html/*

# Copy built frontend assets
COPY --from=build /app/dist /usr/share/nginx/html

# Copy custom Nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 5173

CMD ["nginx", "-g", "daemon off;"]
