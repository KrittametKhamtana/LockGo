# Vite bakes VITE_* env vars in at build time, not runtime — this image is
# therefore specific to whatever VITE_API_BASE_URL it was built with. Fine
# for this project's single deployment target; see docs/DEPLOYMENT.md for
# what changes if that ever stops being true. CI always passes the real
# value explicitly (see .github/workflows/ci.yml); this default only
# matters for a manual `docker build` with no --build-arg.
ARG VITE_API_BASE_URL=http://localhost:8081/api

FROM node:22-alpine AS build
WORKDIR /src

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:alpine AS runtime
COPY --from=build /src/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
