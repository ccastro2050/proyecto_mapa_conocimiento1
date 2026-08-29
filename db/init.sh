#!/bin/bash
# ==============================================================
# Inicializador de SQL Server. SQL Server NO ejecuta los scripts que se le
# monten: alguien tiene que conectarse y correrlos. Este contenedor lo hace
# UNA vez y termina. Es idempotente.
# La contraseña llega por MSSQL_SA_PASSWORD, no está escrita aquí.
# ==============================================================
set -e
SQLCMD=/opt/mssql-tools18/bin/sqlcmd
SERVER=sqlserver
DB=mapa_local

echo "[init] ¿Existe ya la base $DB?"
EXISTE=$($SQLCMD -S $SERVER -U sa -P "$MSSQL_SA_PASSWORD" -C -h -1 -W \
  -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = '$DB'")
if [ "$EXISTE" = "1" ]; then
    echo "[init] Ya existe. No se hace nada."; exit 0
fi
echo "[init] Creando la base $DB..."
$SQLCMD -S $SERVER -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "CREATE DATABASE $DB"
echo "[init] Ejecutando mapa_conocimiento.sql (21 tablas y los catálogos)..."
$SQLCMD -S $SERVER -U sa -P "$MSSQL_SA_PASSWORD" -C -d $DB -i /scripts/mapa_conocimiento.sql
echo "[init] Listo: la base quedó creada y sembrada."
