"""
AI Bridge - traduce un comando in linguaggio naturale in un nodo/artifact
valido del grafo (building.json) e scrive il risultato in target.json,
che verrà letto da JaCaMo.

Uso:
    python ai_bridge.py
    > portami in laboratorio
    -> scritto target.json con {"target": "Laboratorio3"}
"""

import json
import os
import difflib
import re
import google.generativeai as genai

genai.configure(api_key=os.environ["GEMINI_API_KEY"])
gemini_model = genai.GenerativeModel("gemini-2.5-flash")

# --- Configurazione percorsi --------------------------------------------
GRAPH_DIR = os.path.join(
    os.path.expandvars("%USERPROFILE%"),
    "AppData", "LocalLow", "DefaultCompany", "JaCaMoIntegration", "graph_snapshots"
)
TARGET_JSON = os.path.join(GRAPH_DIR, "target.json")

def find_building_json():
    """Trova il file .json del grafo nella cartella (escludendo target.json),
    prendendo il più recente per data di modifica."""
    candidates = []
    for fname in os.listdir(GRAPH_DIR):
        if (fname.lower().endswith(".json")
                and fname != "target.json"
                and fname != "pathfinder_state.json"
                and not fname.lower().endswith("_artifacts.json")):
            full = os.path.join(GRAPH_DIR, fname)
            candidates.append(full)

    if not candidates:
        raise FileNotFoundError("Nessun file .json del grafo trovato in " + GRAPH_DIR)

    candidates.sort(key=os.path.getmtime, reverse=True)
    return candidates[0]


def load_valid_nodes():
    """Legge il file del grafo e ritorna la lista dei nomi dei nodi (stanze, pole, ecc.)."""
    building_json = find_building_json()
    print(f"File grafo trovato: {building_json}")

    with open(building_json, "r", encoding="utf-8") as f:
        data = json.load(f)

    names = set()

    for floor in data.get("floors", []):
        for n in floor.get("nodes", []):
            node_type = n.get("nodeType", "")
            if node_type in ("CorridorPoleA", "CorridorPoleB"):
                corridor_id = n.get("corridorId")
                if corridor_id:
                    names.add(corridor_id)
            else:
                node_id = n.get("nodeId")
                if node_id:
                    names.add(node_id)

            for a in n.get("artifacts", []):
                if isinstance(a, str):
                    names.add(a)
                elif isinstance(a, dict):
                    a_name = a.get("id") or a.get("name")
                    if a_name:
                        names.add(a_name)

    return sorted(names)


def _local_match(user_input: str, valid_names: list[str]) -> str | None:
    """Fuzzy matching locale di fallback (no API)."""
    text = user_input.lower()
    text_norm = re.sub(r"[^a-z0-9]", "", text)

    best_match = None
    best_score = 0.0

    for name in valid_names:
        name_lower = name.lower()
        name_compact = re.sub(r"[^a-z0-9]", "", name_lower)

        if name_compact in text_norm:
            return name

        parts = re.findall(r"[a-z]+|\d+", name_lower)
        if len(parts) > 1 and all(p in text for p in parts):
            return name

        score = difflib.SequenceMatcher(None, name_compact, text_norm).ratio()
        if score > best_score:
            best_score = score
            best_match = name

    if best_score >= 0.5:
        return best_match
    return None


def ask_llm_for_target(user_input: str, valid_names: list[str]) -> str | None:
    """Chiede a Gemini di mappare l'input utente su un nome valido.
    Se l'LLM non è disponibile (errore/quota), usa il matching locale."""
    system_prompt = (
        "Sei un traduttore di comandi per un agente robotico in un edificio.\n"
        "Riceverai un comando in linguaggio naturale e una lista di nomi validi "
        "(stanze e oggetti/artifact).\n"
        "Devi rispondere SOLO con uno dei nomi esatti dalla lista, senza nient'altro:\n"
        "niente spiegazioni, niente virgolette, niente punteggiatura.\n"
        "Se non riesci a determinare con sufficiente sicurezza a quale nome della lista "
        "corrisponda il comando, rispondi esattamente con la parola: NONE\n\n"
        "Nomi validi:\n" + "\n".join(valid_names)
    )

    full_prompt = system_prompt + "\n\nComando utente: " + user_input

    try:
        response = gemini_model.generate_content(full_prompt)
        text = response.text.strip()
    except Exception as e:
        print(f"[AI bridge] LLM non disponibile ({e}), uso matching locale.")
        return _local_match(user_input, valid_names)

    if text == "NONE" or text not in valid_names:
        return None
    return text


def write_target(target: str):
    with open(TARGET_JSON, "w", encoding="utf-8") as f:
        json.dump({"target": target}, f, ensure_ascii=False, indent=2)


def main():
    print("=== AI Bridge ===")
    print(f"Cerco il file del grafo in: {GRAPH_DIR}")

    try:
        valid_names = load_valid_nodes()
    except FileNotFoundError as e:
        print(f"ERRORE: {e}")
        return

    if not valid_names:
        print("ATTENZIONE: nessun nodo trovato in building.json (lista vuota).")
        print("Controlla la struttura del JSON, potrebbe servire un piccolo adattamento.")
        return

    print(f"Nodi/artifact validi trovati ({len(valid_names)}):")
    for n in valid_names:
        print(f"  - {n}")

    print("\nScrivi un comando (es. 'portami in laboratorio'), oppure 'exit' per uscire.")

    while True:
        user_input = input("\n> ").strip()
        if user_input.lower() in ("exit", "quit"):
            break
        if not user_input:
            continue

        target = ask_llm_for_target(user_input, valid_names)

        if target is None:
            print("Non ho capito a quale stanza/oggetto ti riferisci. Riprova.")
            continue

        write_target(target)
        print(f"-> Target impostato: {target}  (scritto in {TARGET_JSON})")


if __name__ == "__main__":
    main()