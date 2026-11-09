from pathlib import Path

import joblib
import pandas as pd
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics import accuracy_score, classification_report, f1_score
from sklearn.model_selection import StratifiedGroupKFold, cross_val_predict
from sklearn.pipeline import Pipeline
from sklearn.tree import DecisionTreeClassifier, export_text

folder = Path(__file__).resolve().parent
data = pd.read_csv(folder / "dau_chan_di_san_intents.csv", encoding="utf-8-sig")

questions = data["CauHoi"]
labels = data["Nhan"]
groups = data["MaNhom"]

model = Pipeline([
    ("tfidf", TfidfVectorizer(ngram_range=(1, 2), min_df=2)),
    ("tree", DecisionTreeClassifier(
        criterion="entropy",
        max_depth=6,
        min_samples_leaf=2,
        random_state=42,
    )),
])

cv = StratifiedGroupKFold(n_splits=3, shuffle=True, random_state=42)
predictions = cross_val_predict(model, questions, labels, groups=groups, cv=cv)

print(f"Accuracy: {accuracy_score(labels, predictions):.2%}")
print(f"F1 macro: {f1_score(labels, predictions, average='macro'):.3f}")
print(classification_report(labels, predictions, zero_division=0))

# Sau khi đánh giá, huấn luyện lại trên toàn bộ dữ liệu để lưu mô hình.
model.fit(questions, labels)
classifier = model.named_steps["tree"]
tree = classifier.tree_
features = model.named_steps["tfidf"].get_feature_names_out()

left, right = tree.children_left[0], tree.children_right[0]
if left != -1:
    weights = tree.weighted_n_node_samples
    entropy_after = (
        weights[left] * tree.impurity[left]
        + weights[right] * tree.impurity[right]
    ) / weights[0]

    print(f"Đặc trưng ở nút gốc: {features[tree.feature[0]]}")
    print(f"Ngưỡng TF-IDF: {tree.threshold[0]:.4f}")
    print(f"Entropy trước khi chia: {tree.impurity[0]:.4f}")
    print(f"Entropy sau khi chia: {entropy_after:.4f}")
    print(f"Information Gain: {tree.impurity[0] - entropy_after:.4f} bit")

print(export_text(classifier, feature_names=features.tolist(), max_depth=3))
joblib.dump(model, folder / "intent_tree.joblib")

example = "Tôi có 2 triệu, muốn đi Huế 2 ngày cho hai người."
print("Câu thử:", example)
print("Nhãn dự đoán:", model.predict([example])[0])