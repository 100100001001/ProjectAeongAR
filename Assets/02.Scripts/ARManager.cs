﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation; // ARFoundation을 사용하기 위해 네임스페이스 선언
using UnityEngine.XR.ARSubsystems;
using TMPro;


public class ARManager : MonoBehaviour
{
    public static ARManager instance;   // ARManager 인스턴스화

    public ARRaycastManager arRaycater; // 평면을 인식하기 위한 ARRaycastManager 변수

    public GameObject[] indicator;      // 평면에 표시될 오브젝트
    // Egg, Baby, Child, Youth, Ground(동산), EggBreak_Top, EggBreak_Bottom, EggBreak 순으로 들어가있다.

    private Transform indicatorTr;      // 사용자의 손 터치로 평면에 표시된 오브젝트 위치가 변경되기 때문에 transform을 변수로 받아줌
    List<ARRaycastHit> indicatorHits = new List<ARRaycastHit>(); // AR raycast에서 쏜 광선


    // 랜덤 색상을 적용하기 위해 캐릭터 각각의 MeshRenderer, Renderer 변수 만듦
    MeshRenderer eggMR;
    Renderer babyMR;
    Renderer childMR;
    Renderer youthMR;
    MeshRenderer eggbreakUpMR;
    MeshRenderer eggbreakBotMR;
    Color color;

    // 색상 배열과 각각의 색들을 지정해줌
    string[] colorNames = new string[8] { "연두색", "노란색", "주황색", "빨간색", "분홍색", "보라색", "남색", "하늘색" };
    Color color1 = new Color32(233, 200, 218, 255); // 연두색 (원래 색상)
    Color color2 = new Color32(255, 255, 218, 255); // 노란색
    Color color3 = new Color32(255, 235, 218, 255); // 주황색
    Color color4 = new Color32(255, 218, 225, 255); // 빨간색
    Color color5 = new Color32(255, 218, 255, 255); // 분홍색
    Color color6 = new Color32(239, 218, 255, 255); // 보라색
    Color color7 = new Color32(218, 221, 255, 255); // 남색
    Color color8 = new Color32(218, 251, 255, 255); // 하늘색


    public TextMeshProUGUI descriptiveText; // 게임 시작할 때 나올 설명 텍스트
    bool textActive = true;                 // 텍스트 활성화 여부. 게임 시작할 때만 나와야하기 때문에 true


    public TextMeshProUGUI colorTest;


    void Start()
    {
        // 캐릭터의 색상을 지정해주기 위해 각각의 MeshRenderer, Renderer컴포넌트를 가져온다
        eggMR = indicator[0].GetComponent<MeshRenderer>();
        babyMR = indicator[1].transform.GetChild(1).GetComponent<Renderer>();
        childMR = indicator[2].transform.GetChild(1).GetComponent<Renderer>();
        youthMR = indicator[3].transform.GetChild(1).GetComponent<Renderer>();
        eggbreakUpMR = indicator[5].GetComponent<MeshRenderer>();
        eggbreakBotMR = indicator[6].GetComponent<MeshRenderer>();

        indicator[0].SetActive(false);
        indicator[1].SetActive(false);
        indicator[2].SetActive(false);
        indicator[3].SetActive(false);
        indicator[7].SetActive(false);
        
        descriptiveText.text = "두 손가락을 동시에 터치해서 또바기를 불러오세요";
        descriptiveText.gameObject.SetActive(true);

        // 캐릭터의 색상 변경
        ChangeColor();

        // AI로 분류 후 모델 값이 있을 경우
        if (GetInferenceFromModel.resultValue >= 0)
        {
            AIColorChange();
        }

        descriptiveText.gameObject.GetComponent<Button>().interactable = false;
    }

    void Update()
    {
        if (Input.touchCount > 1)
        {
            // 3D동산 오브젝트 활성화
            indicator[4].SetActive(true);

            // 동산의 위치를 indicatorTr 위치에 넣어주고 위치 업데이트
            indicatorTr = indicator[4].transform;
            PlaceIndicator();

            // 설명 텍스트 활성화
            if (textActive) StartCoroutine(TextActive());
            textActive = false;
        }

        // AI로 분류 후 모델 값이 있을 경우
        if (GetInferenceFromModel.resultValue >= 0)
        {
            AIColorChange();
        }

        switch (Status.instance.evo1)
        {
            case Status.Evolution1.EGG:
                indicator[0].SetActive(true);
                return;

            case Status.Evolution1.BABY:
                indicator[7].SetActive(false);
                indicator[1].SetActive(true);
                return;

            case Status.Evolution1.CHILD:
                indicator[1].SetActive(false);
                indicator[2].SetActive(true);
                return;

            case Status.Evolution1.YOUTH:
                indicator[2].SetActive(false);
                indicator[3].SetActive(true);
                return;

            case Status.Evolution1.BREAKEGG:
                indicator[0].SetActive(false);
                indicator[7].SetActive(true);
                return;

            default:
                return;
        }

    }


    // 바닥 평면을 감지하는 메서드
    void PlaceIndicator()
    {
        // 화면 중앙에 레이를 쏴서 평면을 찾는다
        // TrackableType.Planes : 광선이 평면 유형과 교차하는 경우 광선이 맞은 것으로 간주
        arRaycater.Raycast(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), indicatorHits, TrackableType.Planes);

        // 감지가 됐다면 indicatorTr의 위치와 회전 정보를 indicatorHits의 pose로 가져온다
        if (indicatorHits.Count > 0)
        {
            indicatorTr.position = indicatorHits[0].pose.position;
            indicatorTr.rotation = indicatorHits[0].pose.rotation;

        }
    }


    // 캐릭터의 색상을 임의 지정하는 메서드
    void ChangeColor()
    {
        // 캐릭터를 보내주었을 때
        if (ByeButton.bye)
        {
            Colors(colorNames[UnityEngine.Random.Range(0, 8)]);
            ApplyColor();
            SaveColor();

            return;
        }

        // 저장된 색상이 있다면 저장된 색상을 가져옴
        if (PlayerPrefs.HasKey("ColorRed"))
        {
            color.r = PlayerPrefs.GetFloat("ColorRed");
            color.g = PlayerPrefs.GetFloat("ColorGreen");
            color.b = PlayerPrefs.GetFloat("ColorBlue");
        }
        // 저장된 색상이 없다면 랜덤한 색상을 가져옴
        else Colors(colorNames[UnityEngine.Random.Range(0, 8)]);

        ApplyColor();
        SaveColor();
    }

    // 시작할 때 설명하는 텍스트 활성화
    IEnumerator TextActive()
    {
        descriptiveText.gameObject.GetComponent<Button>().interactable = true;
        textActive = false;

        descriptiveText.text = "두 손을 꾹 누른 채 움직여 보세요\n또바기의 동산이 따라와요~";
        yield return new WaitForSeconds(7f);

        descriptiveText.text = "또바기와 함께 즐거운 시간 보내세요 >.<";
        yield return new WaitForSeconds(5f);

        descriptiveText.gameObject.SetActive(false);
    }

    // 색상을 저장하는 메서드
    void SaveColor()
    {
        PlayerPrefs.SetFloat("ColorRed", color.r);
        PlayerPrefs.SetFloat("ColorGreen", color.g);
        PlayerPrefs.SetFloat("ColorBlue", color.b);
    }

    // 색상을 적용하는 메서드
    void ApplyColor()
    {
        eggMR.material.SetColor("_Color", color);
        babyMR.material.SetColor("_Color", color);
        childMR.material.SetColor("_Color", color);
        youthMR.material.SetColor("_Color", color);
        eggbreakUpMR.material.SetColor("_Color", color);
        eggbreakBotMR.material.SetColor("_Color", color);
    }

    void Colors(string name)
    {
        switch (name)
        {
            case "연두색":
                color = color1;
                break;
            case "노란색":
                color = color2;
                break;
            case "주황색":
                color = color3;
                break;
            case "빨간색":
                color = color4;
                break;
            case "분홍색":
                color = color5;
                break;
            case "보라색":
                color = color6;
                break;
            case "남색":
                color = color7;
                break;
            case "하늘색":
                color = color8;
                break;
        }
    }

    // AI 모델 분류가 성공했을 때 색상 변경하는 메서드
    void AIColorChange()
    {
        // Red로 분류한 결과값이 가장 높은 경우이다. 빨간색 계열의 색 중 랜덤하여 color값을 지정한다.
        //if (GetInferenceFromModel.instance.prediction.predictedValue == 0)
        if (GetInferenceFromModel.resultValue == 0)
        {
            color = new Color32(225, (byte)UnityEngine.Random.Range(0, 101), (byte)UnityEngine.Random.Range(0, 256), 255);
        }
        // Green으로 분류한 결과값이 가장 높은 경우이다. 초록색 계열의 색 중 랜덤하여 color값을 지정한다.
        //else if (GetInferenceFromModel.instance.prediction.predictedValue == 1)
        else if (GetInferenceFromModel.resultValue == 1)
        {
            color = new Color32((byte)UnityEngine.Random.Range(0, 256), 255, (byte)UnityEngine.Random.Range(0, 101), 255);
        }
        // Blue로 분류한 결과값이 가장 높은 경우이다. 파란색 계열의 색 중 랜덤하여 color값을 지정한다.
        //else if (GetInferenceFromModel.instance.prediction.predictedValue == 2)s
        else if (GetInferenceFromModel.resultValue == 2)
        {
            color = new Color32((byte)UnityEngine.Random.Range(0, 101), (byte)UnityEngine.Random.Range(0, 256), 255, 255);
        }

        colorTest.text = GetInferenceFromModel.resultValue + "/" + color;

        GetInferenceFromModel.resultValue = -1; // 결과값 초기화

        // 변경된 색상을 적용하고 저장한다.
        ApplyColor();
        SaveColor();
    }
}

