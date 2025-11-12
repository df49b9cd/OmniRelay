#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MANIFEST_PATH="${SCRIPT_DIR}/.artifacts/linux-arm64-protoc-path.txt"

if [[ ! -s "${MANIFEST_PATH}" ]]; then
  echo "[protoc-wrapper] Missing manifest at ${MANIFEST_PATH}. Run dotnet restore/build once to generate it." >&2
  exit 90
fi

REAL_PROTOC="$(head -n1 "${MANIFEST_PATH}")"
if [[ ! -x "${REAL_PROTOC}" ]]; then
  echo "[protoc-wrapper] Resolved protoc binary '${REAL_PROTOC}' is not executable." >&2
  exit 91
fi

declare -a INVOCATION_ARGS=()

expand_rsp() {
  local rsp_file="$1"
  if [[ ! -f "${rsp_file}" ]]; then
    INVOCATION_ARGS+=("@${rsp_file}")
    return
  fi

  mapfile -t _tokens < <(perl -MText::ParseWords -e '
    local $/;
    my $content = <>;
    foreach my $word (shellwords($content)) {
      print "$word\n";
    }
  ' "${rsp_file}")

  for token in "${_tokens[@]}"; do
    process_arg "${token}"
  done
}

process_arg() {
  local arg="$1"
  if [[ "${arg}" == @* && "${#arg}" -gt 1 ]]; then
    local file_path="${arg:1}"
    expand_rsp "${file_path}"
    return
  fi

  INVOCATION_ARGS+=("${arg}")
}

for arg in "$@"; do
  process_arg "${arg}"
done

exec "${REAL_PROTOC}" "${INVOCATION_ARGS[@]}"
