#!/usr/bin/env python3
"""Dërgon një email alarmi te pronari i dyqanit.

Përdorim:  alert.py "Titulli" "Teksti"
Kredencialet lexohen nga /etc/ame/api.env (të njëjtat të emailit të porosive); nuk ruhet asgjë tjetër.
"""
import os
import smtplib
import socket
import sys
import time
from email.message import EmailMessage


def load_env(path):
    values = {}
    try:
        with open(path, encoding="utf-8") as file:
            for line in file:
                line = line.strip()
                if line and not line.startswith("#") and "=" in line:
                    key, value = line.split("=", 1)
                    values[key.strip()] = value.strip()
    except OSError:
        pass
    return values


def main():
    if len(sys.argv) < 2:
        sys.exit("Përdorim: alert.py \"Titulli\" \"Teksti\"")
    subject = sys.argv[1]
    body = sys.argv[2] if len(sys.argv) > 2 else ""

    env = load_env(os.environ.get("AME_ENV_FILE", "/etc/ame/api.env"))
    user = env.get("Email__Username", "")
    password = env.get("Email__Password", "")
    host = os.environ.get("AME_ALERT_HOST") or env.get("Email__Host", "smtp.gmail.com")
    port = int(os.environ.get("AME_ALERT_PORT") or env.get("Email__Port", "587"))
    plain = bool(os.environ.get("AME_ALERT_NO_TLS"))
    recipient = env.get("Email__ShopAddress") or user
    if not recipient:
        sys.exit("Nuk ka adresë për alarmin (Email__Username mungon).")

    message = EmailMessage()
    message["Subject"] = f"[amé watches] {subject}"
    message["From"] = user or recipient
    message["To"] = recipient
    message.set_content(f"{body}\n\nServeri: {socket.gethostname()}\nKoha: {time.strftime('%Y-%m-%d %H:%M:%S %Z')}\n")

    try:
        with smtplib.SMTP(host, port, timeout=30) as smtp:
            if not plain:
                smtp.starttls()
                if password:
                    smtp.login(user, password)
            smtp.send_message(message)
    except Exception as error:  # noqa: BLE001 - alarmi nuk duhet të shkaktojë gabim të ri
        print(f"Email-i i alarmit dështoi: {error}", file=sys.stderr)
        sys.exit(1)
    print(f"Alarmi u dërgua te {recipient}: {subject}")


if __name__ == "__main__":
    main()
