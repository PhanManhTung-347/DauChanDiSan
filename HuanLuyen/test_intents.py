from pathlib import Path

import joblib

folder = Path(__file__).resolve().parent
model = joblib.load(folder / "intent_tree.joblib")

test_cases = [
    ("Ở Quảng Nam cũ có di tích nào đáng tham quan?", "TIM_DI_TICH"),
    ("Cho mình danh sách các lăng vua ở Huế.", "TIM_DI_TICH"),

    ("Xếp lịch khám phá Huế hai ngày một đêm cho hai người.", "LAP_HANH_TRINH"),
    ("Lập chuyến Hội An 3 ngày, có chỗ ăn và chỗ ở.", "LAP_HANH_TRINH"),

    ("Hai người đi Huế cuối tuần, cả nhóm có 2 triệu, xếp lịch giúp mình.",
     "LAP_THEO_NGAN_SACH"),
    ("Mỗi người chỉ có 800 nghìn, lên lịch tham quan Hội An một ngày.",
     "LAP_THEO_NGAN_SACH"),

    ("Đổi chỗ ở trong lịch vừa tạo để cả chuyến còn dưới 2 triệu.",
     "SUA_HANH_TRINH"),
    ("Giữ nguyên ngày đầu, chuyển chuyến thăm lăng sang ngày thứ hai.",
     "SUA_HANH_TRINH"),
]

questions = [question for question, _ in test_cases]
predictions = model.predict(questions)
correct = 0

for (question, expected), predicted in zip(test_cases, predictions):
    is_correct = predicted == expected
    correct += int(is_correct)

    print(f"[{'ĐÚNG' if is_correct else 'SAI'}] {question}")
    print(f"  Mong đợi: {expected}")
    print(f"  Dự đoán:  {predicted}\n")

print(f"Kết quả kiểm tra nhanh: {correct}/{len(test_cases)} câu đúng")