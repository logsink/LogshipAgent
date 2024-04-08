#!/bin/sh

usage() {
    cat <<EOF
logship agent installer

Usage: install-agent.sh [OPTIONS]

Options:
  -p
          [default: /opt/logship/agent] Set install directory
      --no-install
          Skip systemd service install
  -h, --help
          Print this help message
EOF
}

service_exists () {
    if [ $(systemctl status $1 2> /dev/null | grep -Fq "Active:") ]; then
        return 1
    else
        return 0
    fi
}

ensure() {
    if ! "$@"; then err "command failed: $*"; fi
}

say() {
    printf 'logship: %s\n' "$1"
}

err() {
    printf "error: %s\n" "$1" >&2
    exit 1
}

need_cmd() {
    if ! check_cmd "$1"; then
        err "need '$1' (command not found)"
    fi
}

need_cmds() {
    missing_cmds=""
    for cmd in "$@"; do
        if ! check_cmd "$cmd"; then
            if [ -z "$missing_cmds" ]; then
                missing_cmds="$cmd"
            else
                missing_cmds="$missing_cmds, $cmd"
            fi
        fi
    done

    if [ -n "$missing_cmds" ]; then
        err "commands not found: $missing_cmds"
    fi
}

check_cmd() {
    command -v "$1" > /dev/null 2>&1
}

main() {
    service_name="logship-agent"
    CONFIG="appsettings.json"
    EXE="Logship.Agent.ConsoleHost"
    FILE="LogshipAgent-linux-x64.zip"
    TEMPDIR="$(mktemp -d -t 'logship-agent-XXXXXXXXXXXXXXXX')"
    INSTALLPATH="/opt/logship/agent"
    INSTALL=true

    for arg in "$@"; do
        case "$arg" in
            --help)
                usage
                exit 0
                ;;
            --no-install)
                INSTALL=false
                ;;
            -p)
                getopt ":p" opt
                INSTALLPATH="${OPTARG%%/}"
                ;;
            *)
                ;;
        esac
    done

    say "initializing installer. Visit https://logship.io/docs/category/agent for documentation and configuration options."
    need_cmds chmod mkdir mktemp rm rmdir unzip wget
    if [ "$(id -u)" -ne 0 ]; then
        err "install script must be run as root."
        
        # shellcheck disable=SC2317
        exit 2
    fi

    say "downloading and staging agent release archive"
    ensure mkdir -p "$INSTALLPATH"
    ensure wget -q --show-progress "https://github.com/logship-io/logship-agent/releases/latest/download/$FILE" -P "$TEMPDIR"
    ensure unzip -qq "$TEMPDIR/$FILE" -d "$TEMPDIR/extract"

    ensure cp -f "$TEMPDIR/extract/$EXE" "$INSTALLPATH/$EXE"
    ensure chmod +x "$INSTALLPATH/$EXE"
    if [ ! -e "$INSTALLPATH/$CONFIG" ]; then
        cp -f "$TEMPDIR/extract/$CONFIG" "$INSTALLPATH/$CONFIG"
        say "edit configuration"
        editor "$INSTALLPATH/$CONFIG"
    fi

    if $INSTALL; then
        if [ "$(ps -p 1 -o comm=)" != "systemd" ]; then
            say "systemd not detected on this system, install not supported"
        else
            say "installing $service_name"
            if [ ! -e "$INSTALLPATH/$CONFIG" ]; then
                say "adding unit file"
                ensure cat >/lib/systemd/system/$service_name.service << EOF
[Unit]
Description=Logship Agent

[Service]
ExecStart=$INSTALLPATH/$EXE
WorkingDirectory=$INSTALLPATH
Restart=on-failure
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
EOF
            fi
            ensure systemctl daemon-reload
            say "logship-agent.service installed"
            say "to enable:"
            say "    systemctl daemon-reload && systemctl enable $service_name && systemctl start $service_name"
        fi
    else
        say "skipping installation"
    fi

    say "cleaning up temporary files..."
    ensure rm -rf "$TEMPDIR"
}

main "$@" || exit 1