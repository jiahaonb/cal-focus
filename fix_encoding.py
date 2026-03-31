# -*- coding: utf-8 -*-
import os

files_to_fix = [
    r"d:\Recently\cal-focus\src\CalFocus.App\Views\Pages\CalendarSchedulePage.xaml.cs",
    r"d:\Recently\cal-focus\src\CalFocus.App\Views\Pages\CalendarSchedulePage.xaml"
]

for file_path in files_to_fix:
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
            print(f"{os.path.basename(file_path)} is already UTF-8")
    except UnicodeDecodeError:
        print(f"Fixing encoding for {os.path.basename(file_path)}...")
        with open(file_path, 'r', encoding='gbk') as f:
            content = f.read()
        
        # fix warning CS0169 in code behind
        if file_path.endswith('.cs'):
            content = content.replace("private Guid? _lastTappedScheduleId;", "")

        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print("Success.")

