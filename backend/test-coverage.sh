#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
COVERAGE_DIR="$SCRIPT_DIR/tests/CoverageResults"
REPORT_DIR="$SCRIPT_DIR/tests/CoverageReport"

echo "=== Rodando testes com cobertura ==="
dotnet test "$SCRIPT_DIR/tests/ProjectManagerWeb.Tests/ProjectManagerWeb.Tests.csproj" \
  --collect:"XPlat Code Coverage" \
  --settings "$SCRIPT_DIR/Coverage.runsettings" \
  --results-directory "$COVERAGE_DIR" \
  --nologo \
  --verbosity minimal

echo "=== Gerando relatório HTML ==="
dotnet reportgenerator \
  "-reports:$COVERAGE_DIR/**/coverage.cobertura.xml" \
  "-targetdir:$REPORT_DIR" \
  -reporttypes:Html

echo ""
echo "Relatório de cobertura:"
echo "  $REPORT_DIR/index.html"
echo ""

# Extrai a porcentagem total do último relatório gerado
if [ -f "$REPORT_DIR/index.html" ]; then
  COVERAGE=$(grep -oP 'Line coverage: \K[0-9.]+(?=%)' "$REPORT_DIR/index.html" | head -1 || echo "N/A")
  echo "Cobertura de linha: $COVERAGE%"
fi
