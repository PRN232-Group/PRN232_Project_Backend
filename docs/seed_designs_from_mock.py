# Extract mock interiorDesigns → seed via API (JSON bodies)
# Usage: python seed_designs_from_mock.py after BE is up

from __future__ import annotations

import json
import re
import urllib.request
from pathlib import Path

MOCK = Path(r"d:\PRN232\Frontend_Project\src\infrastructure\mock\data.js")
BASE = "http://localhost:5259"
ADMIN_EMAIL = "ngthanhtrung302005@gmail.com"
ADMIN_PASSWORD = "Admin@123"


def extract_designs(text: str) -> list[dict]:
    # Find interiorDesigns array start
    m = re.search(r"interiorDesigns:\s*\[", text)
    if not m:
        raise SystemExit("interiorDesigns not found")
    i = m.end() - 1  # at '['
    depth = 0
    start = i
    for j in range(i, len(text)):
        c = text[j]
        if c == "[":
            depth += 1
        elif c == "]":
            depth -= 1
            if depth == 0:
                arr = text[start : j + 1]
                break
    else:
        raise SystemExit("unclosed array")

    # Convert JS object-ish to JSON: quote keys, single→double already mostly double
    js = arr
    # remove trailing commas
    js = re.sub(r",\s*}", "}", js)
    js = re.sub(r",\s*]", "]", js)
    # quote unquoted keys (including nested)
    js = re.sub(r"([{\[,]\s*)([A-Za-z_][A-Za-z0-9_]*)\s*:", r'\1"\2":', js)
    return json.loads(js)


def to_upsert(d: dict) -> dict:
    return {
        "title": d.get("title"),
        "category": d.get("category"),
        "style": d.get("style"),
        "imageUrl": d.get("imageUrl"),
        "gallery": d.get("gallery") or [],
        "description": d.get("description"),
        "areaSqm": d.get("areaSqm"),
        "budgetFrom": d.get("budgetFrom"),
        "budgetTo": d.get("budgetTo"),
        "timelineWeeks": d.get("timelineWeeks"),
        "priceCompare": d.get("priceCompare")
        or {"studio": 0, "marketAvg": 0},
        "relatedProductIds": d.get("relatedProductIds") or [],
        "highlights": d.get("highlights") or [],
        "specs": d.get("specs") or [],
        "materials": d.get("materials") or [],
        "packages": d.get("packages") or [],
        "isPublished": d.get("isPublished", True),
    }


def http_json(method: str, url: str, body=None, token=None):
    data = None if body is None else json.dumps(body).encode("utf-8")
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    with urllib.request.urlopen(req) as res:
        raw = res.read().decode("utf-8")
        return json.loads(raw) if raw else None


def main():
    designs = extract_designs(MOCK.read_text(encoding="utf-8"))
    print(f"found {len(designs)} designs")
    login = http_json(
        "POST",
        f"{BASE}/api/auth/login",
        {"email": ADMIN_EMAIL, "password": ADMIN_PASSWORD},
    )
    token = login.get("accessToken")
    if not token:
        raise SystemExit(f"login failed: {login}")
    existing = http_json("GET", f"{BASE}/api/interior-designs", token=token) or []
    titles = { (x.get("title") or "").strip().lower() for x in existing }
    print(f"existing {len(existing)}")
    for d in designs:
        body = to_upsert(d)
        title = (body.get("title") or "").strip().lower()
        if title in titles:
            print("skip", (body.get("title") or "").encode("ascii", "replace").decode())
            continue
        created = http_json(
            "POST", f"{BASE}/api/interior-designs", body, token=token
        )
        print("created", created.get("id"), (created.get("title") or "").encode("ascii", "replace").decode())
        titles.add(title)


if __name__ == "__main__":
    main()
