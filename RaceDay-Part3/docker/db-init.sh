#!/bin/bash
# Loads the RaceDay schema + Part 3 migration once. Password comes from SQLCMDPASSWORD.
set -e
SQLCMD="/opt/mssql-tools18/bin/sqlcmd -C -S sqlserver -U sa -b"

exists=$($SQLCMD -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('RaceDay') IS NOT NULL AND EXISTS (SELECT 1 FROM RaceDay.sys.tables WHERE name='Events') THEN 1 ELSE 0 END" | tr -d '[:space:]')

if [ "$exists" = "1" ]; then
  echo "RaceDay schema already present - skipping schema script."
else
  echo "Running raceday_schema.sql ..."
  $SQLCMD -i /sql/raceday_schema.sql
fi

# The migration is idempotent (IF NOT EXISTS), so it is always safe to run.
echo "Running raceday_part3_migration.sql ..."
$SQLCMD -i /sql/raceday_part3_migration.sql
echo "Database ready."
