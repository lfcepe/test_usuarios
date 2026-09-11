# Imagen del frontend: se compila con Node y se sirve con nginx.

FROM node:22-alpine AS build
WORKDIR /app

# package.json y el lock primero, para reutilizar la capa de npm ci mientras las
# dependencias no cambien.
COPY package*.json ./
RUN npm ci

COPY . .

# Vite sustituye las variables VITE_ en tiempo de compilacion, no de ejecucion:
# tienen que estar presentes aqui y no en el contenedor final.
ARG VITE_FIREBASE_API_KEY=""
ARG VITE_FIREBASE_AUTH_DOMAIN=""
ARG VITE_FIREBASE_PROJECT_ID=""
ARG VITE_FIREBASE_STORAGE_BUCKET=""
ARG VITE_FIREBASE_MESSAGING_SENDER_ID=""
ARG VITE_FIREBASE_APP_ID=""
ARG VITE_FIREBASE_MEASUREMENT_ID=""

ENV VITE_FIREBASE_API_KEY=$VITE_FIREBASE_API_KEY \
    VITE_FIREBASE_AUTH_DOMAIN=$VITE_FIREBASE_AUTH_DOMAIN \
    VITE_FIREBASE_PROJECT_ID=$VITE_FIREBASE_PROJECT_ID \
    VITE_FIREBASE_STORAGE_BUCKET=$VITE_FIREBASE_STORAGE_BUCKET \
    VITE_FIREBASE_MESSAGING_SENDER_ID=$VITE_FIREBASE_MESSAGING_SENDER_ID \
    VITE_FIREBASE_APP_ID=$VITE_FIREBASE_APP_ID \
    VITE_FIREBASE_MEASUREMENT_ID=$VITE_FIREBASE_MEASUREMENT_ID

RUN npm run build


FROM nginx:1.27-alpine AS runtime

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

# Se comprueba contra 127.0.0.1 y no contra localhost: dentro del contenedor
# localhost resuelve primero a ::1 y nginx solo escucha en IPv4, asi que la
# comprobacion fallaba con "connection refused" mientras el sitio se servia
# perfectamente por el puerto publicado.
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://127.0.0.1/health || exit 1
