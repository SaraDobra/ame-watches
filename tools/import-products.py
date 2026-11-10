#!/usr/bin/env python3
"""Krijon ora në dyqan nga një skedar JSON dhe ngarkon fotot përmes API-t të adminit.

Përdorim:
  python3 tools/import-products.py --manifest ~/Desktop/watches/ready/produktet.json --dry-run
  python3 tools/import-products.py --manifest ~/Desktop/watches/ready/produktet.json --url https://amewatches.com

Fjalëkalimi i adminit kërkohet në terminal (nuk shfaqet dhe nuk ruhet).
Variabla AME_ADMIN_PASSWORD përdoret vetëm për prova automatike lokale.
Një orë që ekziston tashmë (i njëjti kod modeli) kalohet, kështu nuk krijohen dyfish.
"""
import argparse
import getpass
import json
import mimetypes
import os
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid

SHOP_EMAIL = "ame.accessories.ks@gmail.com"


def call(base, method, path, token=None, body=None, data=None, content_type="application/json"):
    headers = {}
    if token:
        headers["Authorization"] = "Bearer " + token
    if body is not None:
        data = json.dumps(body).encode()
    if data is not None:
        headers["Content-Type"] = content_type
    request = urllib.request.Request(base + path, data=data, method=method, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=120) as response:
            text = response.read().decode()
            return response.status, (json.loads(text) if text else None)
    except urllib.error.HTTPError as error:
        text = error.read().decode(errors="replace")
        try:
            return error.code, json.loads(text)
        except ValueError:
            return error.code, text[:300]


def multipart(paths):
    boundary = uuid.uuid4().hex
    chunks = []
    for path in paths:
        mime = mimetypes.guess_type(path)[0] or "application/octet-stream"
        chunks.append(
            (f'--{boundary}\r\nContent-Disposition: form-data; name="files"; filename="{os.path.basename(path)}"\r\n'
             f"Content-Type: {mime}\r\n\r\n").encode())
        with open(path, "rb") as file:
            chunks.append(file.read())
        chunks.append(b"\r\n")
    chunks.append(f"--{boundary}--\r\n".encode())
    return b"".join(chunks), f"multipart/form-data; boundary={boundary}"


def message(response):
    if isinstance(response, dict):
        if response.get("message"):
            return response["message"]
        if isinstance(response.get("errors"), dict):
            return " ".join(m for items in response["errors"].values() for m in items)
    return str(response)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--url", default="http://localhost:5260", help="adresa e dyqanit, p.sh. https://amewatches.com")
    parser.add_argument("--email", default=SHOP_EMAIL)
    parser.add_argument("--dry-run", action="store_true", help="tregon planin pa u lidhur me dyqanin")
    args = parser.parse_args()

    manifest = os.path.abspath(os.path.expanduser(args.manifest))
    root = os.path.dirname(manifest)
    with open(manifest, encoding="utf-8") as file:
        products = json.load(file)["products"]

    problems = []
    for product in products:
        for image in product["images"]:
            if not os.path.isfile(os.path.join(root, image)):
                problems.append(f'{product["modelCode"]}: mungon fotoja {image}')
    if problems:
        sys.exit("Gabime te skedari:\n  " + "\n  ".join(problems))

    print(f"{len(products)} ora, {sum(len(p['images']) for p in products)} foto:")
    for p in products:
        state = "e dukshme" if p["isActive"] else "e fshehur"
        print(f'  {p["modelCode"]:<16} {p["name"]:<28} {p["price"]:>6} €  {p["category"] or "-":<26} {len(p["images"])} foto, {state}')
    if args.dry_run:
        print("(provë: nuk u krijua asgjë)")
        return

    base = args.url.rstrip("/")
    password = os.environ.get("AME_ADMIN_PASSWORD") or getpass.getpass(f"Fjalëkalimi i adminit ({args.email}): ")
    status, login = call(base, "POST", "/api/auth/login", body={"email": args.email, "password": password})
    if status != 200:
        sys.exit(f"Hyrja dështoi ({status}): {message(login)}")
    token = login["token"]

    brands = {b["slug"]: b["id"] for b in call(base, "GET", "/api/brands")[1]}
    categories = {c["slug"]: c["id"] for c in call(base, "GET", "/api/admin/categories", token)[1]}

    created, skipped, failed = [], [], []
    for p in products:
        code = p["modelCode"]
        _, found = call(base, "GET", "/api/admin/products?search=" + urllib.parse.quote(code), token)
        if any(item["modelCode"] == code for item in found["items"]):
            skipped.append(code)
            print(f"  kalohet   {code} (ekziston tashmë)")
            continue
        if p["brand"] not in brands:
            failed.append(f"{code}: brendi '{p['brand']}' nuk ekziston")
            continue
        if p["category"] and p["category"] not in categories:
            failed.append(f"{code}: kategoria '{p['category']}' nuk ekziston")
            continue

        body = {
            "name": p["name"], "modelCode": code, "brandId": brands[p["brand"]],
            "categoryId": categories.get(p["category"]), "price": p["price"], "stock": p["stock"],
            "gender": p["gender"], "type": p["type"], "caseSizeMm": p.get("caseSizeMm"),
            "waterResistance": p.get("waterResistance"), "movement": p.get("movement"),
            "description": p.get("description"), "isActive": p["isActive"],
        }
        status, result = call(base, "POST", "/api/admin/products", token, body=body)
        if status != 201:
            failed.append(f"{code}: krijimi dështoi ({status}): {message(result)}")
            continue

        paths = [os.path.join(root, image) for image in p["images"]]
        data, content_type = multipart(paths)
        status, uploaded = call(base, "POST", f"/api/admin/products/{result['id']}/images", token, data=data, content_type=content_type)
        if status != 200:
            failed.append(f"{code}: ora u krijua (id {result['id']}) por fotot dështuan ({status}): {message(uploaded)}")
            continue
        created.append(code)
        print(f"  krijuar   {code} ({len(paths)} foto)")

    print(f"\nKrijuar: {len(created)}   Kaluar: {len(skipped)}   Dështuar: {len(failed)}")
    for line in failed:
        print("  GABIM", line)
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
