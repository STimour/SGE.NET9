# 🧭 SGE – Web Service (Backend .NET 9)

## 🇫🇷 Présentation

**SGE** (Système de Gestion des Employés) est une application réalisée dans le cadre du cours **Web Service**.  
Ce dépôt contient la **partie backend**, développée en **C# avec .NET 9**, et expose une API REST permettant la gestion des employés, départements et autres entités métier d’une entreprise.

Le **frontend** sera développé séparément, dans un autre dépôt, avec **React** (ou Angular) en suivant une architecture **Atomic Design**.

### 🎯 Objectif du projet
-  Apprendre à concevoir et implémenter un **web service structuré**, modulaire et maintenable.  
-  Mettre en œuvre les **principes d’architecture logicielle** : séparation des couches, inversion de contrôle (IoC), injection de dépendances (DI).
-  Fournir une API claire et documentée, respectant les bonnes pratiques REST.  

### ⚙️ Stack technique
-   **Langage :** C#
-   **Framework :** .NET 9 (ASP.NET Core Web API)
-   **Architecture :** Controller → Service → Repository (+ interfaces)
-   **Principe clé :** Inversion of Control (IoC) et Dependency Injection (DI)
-   **Base de données :** PostgreSQL *(ou autre selon configuration)*
-   **ORM :** Entity Framework Core
-   **Conteneurisation :** Docker + Docker Compose
  
### 🐳 Configuration Docker
-  Dockerfile
(Build multi-étape pour des images propres et optimisées)
-  docker-compose.yml
(Orchestre à la fois l’API et la base de données PostgreSQL)

### 📁 Structure du projet
SGE/
  ┣ SGE.API/ → API controllers & Program.cs
  ┣ SGE.Application/ → DTOs, services, interfaces
  ┣ SGE.Core/ → Entities et logique métier
  ┗ SGE.Infrastructure/ → Repositories, context EF, migrations

## 🚀 Lancer le projet

### 1. Cloner le dépôt (ssh)
```bash
git clone git@github.com:STimour/SGE.NET9.git
cd SGE
```
### 2. Construire et démarrer les conteneurs
```bash
docker compose up --build
```

### 3. URL de l’API
<a href="http://localhost:8080">http://localhost:8080</a>

### 4. Arrêter les conteneurs
```bash
docker compose down
```


## 🇬🇧 Overview

**SGE** (Employee Management System) is an academic project built for the Web Service course.
This repository contains the backend (Web API) written in C# using .NET 9, exposing a REST API to manage employees, departments, and other organizational data.
The frontend will be developed in a separate repository using React (or Angular) following an Atomic Design architecture.

### 🎯 Project Goals
-  Learn to design and implement a structured and maintainable web service.
-  Apply key software architecture principles: layered design, Inversion of Control (IoC), and Dependency Injection (DI).
-  Provide a clean and well-documented REST API.

### ⚙️ Tech Stack
-  Language: C#
-  Framework: .NET 9 (ASP.NET Core Web API)
-  Architecture: Controller → Service → Repository (+ interfaces)
-  Key Concepts: IoC & DI
-  Database: PostgreSQL
-  ORM: Entity Framework Core
-  Containerization: Docker + Docker Compose

### 🐳 Docker Setup
- Dockerfile
(Multi-stage build for clean and optimized images)
- docker-compose.yml
(Orchestrates both the API and PostgreSQL)

### 📁 Project Structure
SGE/
 ┣ SGE.API/              → Controllers & Program.cs
 ┣ SGE.Application/      → DTOs, services, interfaces
 ┣ SGE.Core/             → Entities & domain logic
 ┗ SGE.Infrastructure/   → Repositories, EF context, migrations

 
## How to Run

### 1. Clone the repository (ssh)
```bash
git clone git@github.com:STimour/SGE.NET9.git
cd SGE
```
### 2. Build and start containers
```bash
docker compose up --build
```

### 3. URL of API
<a href="http://localhost:8080">http://localhost:8080</a>

### 4. Stop containers
```bash
docker compose down
```
