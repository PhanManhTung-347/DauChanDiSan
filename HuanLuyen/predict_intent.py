import json
import sys
from pathlib import Path

import joblib

question = " ".join(sys.argv[1:]).strip()
if not question:
    raise SystemExit("Hãy nhập một câu hỏi.")

model_path = Path(__file__).resolve().parent / "intent_tree.joblib"
model = joblib.load(model_path)
intent = str(model.predict([question])[0])

print(json.dumps({"yDinh": intent}))