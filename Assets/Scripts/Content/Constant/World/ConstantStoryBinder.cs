using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Constant 씬의 대화 바인더 — 씬에 배치된 Dialogue System 의 DialogueRunner 를
/// 전역 StoryManager 에 등록한다. (씬마다 배치 필수 규약)
/// </summary>
public class ConstantStoryBinder : MonoBehaviour
{
    [SerializeField] private DialogueRunner _runner;

    private void Start()
    {
        if (_runner == null)
            _runner = FindFirstObjectByType<DialogueRunner>(FindObjectsInactive.Include);

        if (_runner != null)
            SingletonManagers.Story?.RegisterRunner(_runner);
        else
            Debug.LogWarning("[Constant] DialogueRunner 를 찾지 못했습니다 — 관측자 대사가 비활성화됩니다.");
    }
}
