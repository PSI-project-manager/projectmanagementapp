FROM node:22-alpine

WORKDIR /App

RUN corepack enable

COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY . /App/

EXPOSE 5173
CMD ["pnpm", "dev", "--host", "0.0.0.0"]
