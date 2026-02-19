import re
import os

os.chdir(os.path.dirname(os.path.abspath(__file__)))

def remove_inner_classes(content, class_names):
    lines = content.split(chr(10))
    result_lines = []
    skip = False
    brace_count = 0
    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()
        if not skip:
            should_skip = False
            for cn in class_names:
                pattern = r"^public class " + re.escape(cn) + r""
                if re.match(pattern, stripped):
                    should_skip = True
                    break
            if should_skip:
                skip = True
                brace_count = 0
                while result_lines and result_lines[-1].strip() == "":
                    result_lines.pop()
                brace_count += line.count("{") - line.count("}")
                if brace_count <= 0 and "{" in line and "}" in line:
                    skip = False
                i += 1
                continue
        if skip:
            brace_count += line.count("{") - line.count("}")
            if brace_count <= 0:
                skip = False
            i += 1
            continue
        result_lines.append(line)
        i += 1
    return chr(10).join(result_lines)

def process_file(filename, using_anchor, using_import, replacements, classes_to_remove):
    with open(filename, "r") as f:
        content = f.read()
    content = content.replace(using_anchor, using_anchor + chr(10) + using_import)
    for old, new in replacements:
        content = content.replace(old, new)
    content = remove_inner_classes(content, classes_to_remove)
    with open(filename, "w") as f:
        f.write(content)
    print(filename + " - done")

using_stmt = "using Cornucopia.Core.Models;"

# addUserModel.cs
process_file("addUserModel.cs", "using UnityEngine.UI;", using_stmt, [
    ("model m = JsonUtility.FromJson<model>(", "LegacyModel m = JsonUtility.FromJson<LegacyModel>("),
    ("data d = JsonUtility.FromJson<data>(", "LegacyModelData d = JsonUtility.FromJson<LegacyModelData>("),
    ("data d = new data(", "LegacyModelData d = new LegacyModelData("),
    ("userData ud = JsonUtility.FromJson<userData>(", "LegacyUserData ud = JsonUtility.FromJson<LegacyUserData>("),
    ("userData ud = new userData(", "LegacyUserData ud = new LegacyUserData("),
], ["model", "data", "userData"])

# deleteUserModel.cs
process_file("deleteUserModel.cs", "using UnityEngine.UI;", using_stmt, [
    ("model m = JsonUtility.FromJson<model>(", "LegacyModel m = JsonUtility.FromJson<LegacyModel>("),
    ("data d = JsonUtility.FromJson<data>(", "LegacyModelData d = JsonUtility.FromJson<LegacyModelData>("),
    ("data d = new data(", "LegacyModelData d = new LegacyModelData("),
    ("userData ud = JsonUtility.FromJson<userData>(", "LegacyUserData ud = JsonUtility.FromJson<LegacyUserData>("),
    ("userData ud = new userData(", "LegacyUserData ud = new LegacyUserData("),
], ["model", "data", "userData"])

# displayModelUsers.cs
process_file("displayModelUsers.cs", "using UnityEngine.UI;", using_stmt, [
    ("userDetails m = JsonUtility.FromJson<userDetails>(", "LegacyUserDetails m = JsonUtility.FromJson<LegacyUserDetails>("),
], ["userDetails"])

# displayNModelUsers.cs
process_file("displayNModelUsers.cs", "using UnityEngine.UI;", using_stmt, [
    ("userDetails m = JsonUtility.FromJson<userDetails>(", "LegacyUserDetails m = JsonUtility.FromJson<LegacyUserDetails>("),
    ("userData ud = JsonUtility.FromJson<userData>(", "LegacyUserData ud = JsonUtility.FromJson<LegacyUserData>("),
    ("data da = JsonUtility.FromJson<data>(", "LegacyModelData da = JsonUtility.FromJson<LegacyModelData>("),
    ("data d = new data(", "LegacyModelData d = new LegacyModelData("),
    ("userData ud = new userData(", "LegacyUserData ud = new LegacyUserData("),
], ["userDetails", "userData", "data"])

# displayUserDetails.cs
process_file("displayUserDetails.cs", "using UnityEngine.SceneManagement;", using_stmt, [
    ("userData d = JsonUtility.FromJson<userData>(", "LegacyUserData d = JsonUtility.FromJson<LegacyUserData>("),
], ["userData"])

# displayUsers.cs
process_file("displayUsers.cs", "using UnityEngine.UI;", using_stmt, [
    ("userDetails m = JsonUtility.FromJson<userDetails>(", "LegacyUserDetails m = JsonUtility.FromJson<LegacyUserDetails>("),
], ["userDetails"])

# modelDatabase1.cs
process_file("modelDatabase1.cs", "using EasyUI.Dialogs;", using_stmt, [
    ("model m = JsonUtility.FromJson<model>(", "LegacyModel m = JsonUtility.FromJson<LegacyModel>("),
    ("data d = JsonUtility.FromJson<data>(", "LegacyModelData d = JsonUtility.FromJson<LegacyModelData>("),
], ["model", "data"])

# viewUserModels.cs
process_file("viewUserModels.cs", "using UnityEngine.UI;", using_stmt, [
    ("model m = JsonUtility.FromJson<model>(", "LegacyModel m = JsonUtility.FromJson<LegacyModel>("),
], ["model"])

print("All 8 files updated successfully!")
