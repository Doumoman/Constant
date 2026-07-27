using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

/// <summary>
/// 엑셀로 CSV 파일을 열 때 글씨가 깨지는 현상을 방지해주는 스크립트로 
/// 유니티 에디터의 프로젝트 창에서 CSV 파일을 선택하고 우클릭, 메뉴에 "Fix CSV Encoding for Excel (Add BOM)"을 클릭하여 사용
/// </summary>
public class CSVEncodingFixer : Editor
{
    // 프로젝트 창에서 파일 우클릭 시 메뉴가 뜹니다.
    [MenuItem("Assets/Fix CSV Encoding for Excel (Add BOM)", false, 20)]
    private static void FixEncoding()
    {
        var obj = Selection.activeObject;
        if (obj == null) return;

        string path = AssetDatabase.GetAssetPath(obj);

        // CSV 파일인지 확인
        if (!path.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning("CSV 파일이 아닙니다.");
            return;
        }

        string fullPath = Path.GetFullPath(path);

        try
        {
            // 1. 현재 내용을 읽습니다 (UTF-8로 가정)
            string content = File.ReadAllText(fullPath, Encoding.UTF8);

            // 2. BOM이 포함된 UTF-8로 강제 저장합니다.
            // Encoding.UTF8은 기본적으로 BOM을 안 쓰지만, 
            // new UTF8Encoding(true)를 쓰면 BOM(0xEF,0xBB,0xBF)을 헤더에 붙입니다.
            File.WriteAllText(fullPath, content, new UTF8Encoding(true));

            // 3. 유니티 새로고침
            AssetDatabase.Refresh();

            Debug.Log($"[성공] '{obj.name}' 파일에 BOM을 추가했습니다. 이제 엑셀에서 열어도 안 깨집니다!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"변환 실패: {e.Message}");
        }
    }

    // CSV 파일 선택했을 때만 메뉴 활성화
    [MenuItem("Assets/Fix CSV Encoding for Excel (Add BOM)", true)]
    private static bool ValidateFixEncoding()
    {
        var obj = Selection.activeObject;
        return obj != null && AssetDatabase.GetAssetPath(obj).EndsWith(".csv");
    }
}