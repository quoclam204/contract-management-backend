#!/bin/bash
set -e

echo "=== SQL Server Database Initialization ==="
echo "Target host: $DB_HOST:1433"

SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
if [ ! -f "$SQLCMD" ]; then
    SQLCMD="/opt/mssql-tools/bin/sqlcmd"
fi

echo "Using sqlcmd at: $SQLCMD"

# Wait for SQL Server engine to be fully available
for i in {1..40}; do
    if $SQLCMD -S "$DB_HOST,1433" -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1; then
        echo "SQL Server is responding to queries."
        break
    fi
    echo "Waiting for SQL Server ($i/40)..."
    sleep 2
done

# Check if ContractDb already exists
DB_EXISTS=$($SQLCMD -S "$DB_HOST,1433" -U sa -P "$MSSQL_SA_PASSWORD" -C -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = 'ContractDb'" 2>/dev/null | tr -d '\r\n[:space:]')

if [ "$DB_EXISTS" = "1" ]; then
    echo "Database [ContractDb] already exists. Skipping database schema creation."
else
    echo "Creating database and executing database.sql..."
    $SQLCMD -S "$DB_HOST,1433" -U sa -P "$MSSQL_SA_PASSWORD" -C -i /scripts/database.sql
    echo "Database [ContractDb] initialized successfully from database.sql."
fi

echo "=== Initialization Finished Successfully ==="
