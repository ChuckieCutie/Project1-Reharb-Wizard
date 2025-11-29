using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void PlayNumberGame()
    {
        SceneManager.LoadScene("Scene_Math"); // Tên Scene Game 1
    }

    public void PlayThrowGame()
    {
        SceneManager.LoadScene("SampleScene"); // Tên Scene Game 2 (cũ)
    }
}