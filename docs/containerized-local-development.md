# Containerized Local Development

This guide explains how to run the Azure Event-Driven Platform locally using Docker Compose.

The local containerized environment includes:

- ASP.NET Core Event Ingestion API
- Azure Functions event processor
- Azurite Azure Storage emulator
- Shared Docker network
- Environment-variable-driven configuration

## Purpose

The goal of this setup is to make the platform easier to run consistently across development machines.

It also prepares the project for future container-based deployment patterns such as:

- Azure Container Registry
- Azure Container Apps
- AKS
- container-based CI/CD
- local integration testing

## Prerequisites

Install:

- Docker Desktop
- .NET 8 SDK
- Git

Verify Docker is running:

```bash
docker info

q
