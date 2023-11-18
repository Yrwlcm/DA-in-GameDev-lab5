# АНАЛИЗ ДАННЫХ И ИСКУССТВЕННЫЙ ИНТЕЛЛЕКТ [in GameDev]
Отчет по лабораторной работе #5 выполнил(а):
- Холстинин Егор Алексеевич
- РИ-220943
Отметка о выполнении заданий (заполняется студентом):

| Задание | Выполнение | Баллы |
| ------ | ------ | ------ |
| Задание 1 | * | 60 |
| Задание 2 | * | 20 |
| Задание 3 | * | 20 |

знак "*" - задание выполнено; знак "#" - задание не выполнено;

Работу проверили:
- к.т.н., доцент Денисов Д.В.
- к.э.н., доцент Панов М.А.
- ст. преп., Фадеев В.О.

[![N|Solid](https://cldup.com/dTxpPi9lDf.thumb.png)](https://nodesource.com/products/nsolid)

[![Build Status](https://travis-ci.org/joemccann/dillinger.svg?branch=master)](https://travis-ci.org/joemccann/dillinger)

Структура отчета

- Данные о работе: название работы, фио, группа, выполненные задания.
- Цель работы.
- Задание 1.
- Найдите внутри C# скрипта “коэффициент корреляции” и сделать выводы о том, как он влияет на обучение модели.
- Задание 2.
- Изменить параметры файла yaml-агента и определить какие параметры и как влияют на обучение модели. Привести описание не менее трех параметров.
- Задание 3.
- Приведите примеры, для каких игровых задачи и ситуаций могут использоваться примеры 1 и 2 с ML-Agent’ом. В каких случаях проще использовать ML-агент, а не писать программную реализацию решения? 
- Выводы.
- ✨Magic ✨

## Цель работы
Познакомиться с программными средствами для создания системы машинного обучения и ее интеграции в Unity.

## Предисловие
Хотелось бы начать с небольшого Предисловие не по цели лабораторной работы, но относящегося к ней.
В методичке по лабораторной работе было два проекта с MLAgent-ами: 1-ая с шариком который просто катался за кубиком, и 2-ая с моделью инфляции.
И с первым проектом все было хорошо, шарик довольно быстро обучался и предсказуемо себя вел.
Во втором же проекте даже после всех настроек по методичке шарики подозрительно синхронно двигались, будто бы обучения не происходило, как на скриншоте.

![image](https://github.com/Yrwlcm/DA-in-GameDev-lab5/assets/99079920/00924ff6-e342-4dd5-82b1-3445ebf272a4)

А так же рано или поздно при любых исходных данных график в тензорборде становился таким. А именно резко прыгающим от -1 к +1 по значению награды.

![image](https://github.com/Yrwlcm/DA-in-GameDev-lab5/assets/99079920/4dd73d0d-19f9-446b-9f3f-c1ca28eba065)

Не знаю, в чем на самом деле дело. Но я нашел 2 как по мне ключевые причины из-за чего это происходило. И в последствии, после исправления, перестало происходить.

#### 1 проблема - изначальная синхронность шариков
Все дело вот в этом массиве с инпутами от MlAgent-а
```C#
actionBuffers.ContinuousActions
```
И всех его появлениях

Дело в том, что значения в этом массиве находятся в диапазоне [-1,1]. При этом он везде обернут в функцию 
```C# 
Mathf.Clamp(actionBuffers.ContinuousActions[0], 1f, 10f);
```
Которая возвращает значение только между заданых чисел или одно из крайних.
И лишь у одной функции Clamp эти промежутки заданы до 1. У всех остальных промежуток больше. А значит, единственное значение, которое она может вернуть - это единица.
Решением этой проблемы стало то, что я в каждой такой функции домножил ContinuousActions так, чтобы он попадал в промежуток.

Пример
```C#
Mathf.Clamp(actionBuffers.ContinuousActions[0]*10, 1f, 10f);
```

После этого шарики начали двигаться случайно и обучаться. 

![image](https://github.com/Yrwlcm/DA-in-GameDev-lab5/assets/99079920/45deb7aa-6220-4a29-bd2d-d4ce880ff528)

Однако другая проблема осталась
#### Проблема 2 - скачущий график

Поизучав программу, посмотрев переменные, я заметил, что очень часто происходила ситуация, где tempInf из кода ниже был равен бесконечности. Вряд ли мы работаем с бесконечностями, значит, деление на ноль.
```C#
tempInf = ((pricesMonth[1] - pricesMonth[0]) / pricesMonth[0]) * 100;
if (tempInf is <= 6f)
{
	SetReward(1.0f);
	Debug.Log("True");
	Debug.Log("tempInf: " + tempInf);
	EndEpisode();
}
else
{
	SetReward(-1.0f);
	Debug.Log("False");
	Debug.Log("tempInf: " + tempInf);
	EndEpisode();
}
```

Продолжив разбираться в коде, я заметил, что единственное, где мы напрямую задаем pricesMonth равными нулю, это в начале обучения. Которое вызывается в двух случаях, простите за тавтологию, в начале и при повторном обучении.
```C#
public override void OnEpisodeBegin()
{
	...
	pricesMonth[0] = 0.0f;
	pricesMonth[1] = 0.0f;
	tempInf = 0.0f;
	month = 1;
}
```
Еще полазив в коде, я заметил, что мы можем закончить эпизод обучения только когда month > 2. А единственное, где мы увеличиваем month, это в корутине
```C#
IEnumerator StartMonth()
{
	checkStartMonth = true;
	yield return new WaitForSeconds(60);
	checkStartMonth = false;
	month++;
}
```
Которые мы благополучно запускаем, но нигде не контролируем их завершение. Поэтому, даже после конца эпизода обучения у нас остаются корутины с прошлого эпизода, которые накладываются друг на друга и начинают досрочно завершать обучение.
Решением этой проблемы стала 1 строчка.

```C#
public override void OnEpisodeBegin()
{
	StopAllCoroutines(); //Вот эта
	...
	pricesMonth[0] = 0.0f;
	pricesMonth[1] = 0.0f;
	tempInf = 0.0f;
	month = 1;
}
```
Нам достаточно в начале каждого обучения останавливать все корутины, ошибок при первом обучении это не вызывает, но избавляет нас от ситуаций, где корутины накладываются друг на друга и на эпизоды.

После этих манипуляций хоть модель и не обучилась идеально, но был получен более реалистичный (как по мне) график.
![image](https://github.com/Yrwlcm/DA-in-GameDev-lab5/assets/99079920/278c54f9-50e5-4020-9716-677c67069fcc)

А ниже итоговый код агента.

```C#
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using MathF = System.MathF;

public class Move : Agent
{
	[SerializeField] private GameObject goldMine;
	[SerializeField] private GameObject village;
	private float speedMove;
	private float timeMining;
	private float month;
	private bool checkMiningStart = false;
	private bool checkMiningFinish = false;
	private bool checkStartMonth = false;
	private bool setSensor = true;
	private float amountGold;
	private float pickaxeСost;
	private float profitPercentage;
	private float[] pricesMonth = new float[2];
	private float priceMonth;
	private float tempInf;
	private float tempInf2;

	// Start is called before the first frame update
	public override void OnEpisodeBegin()
	{
		// If the Agent fell, zero its momentum
		if (this.transform.localPosition != village.transform.localPosition)
		{
			this.transform.localPosition = village.transform.localPosition;
		}
		StopAllCoroutines();
		checkMiningStart = false;
		checkMiningFinish = false;
		checkStartMonth = false;
		setSensor = true;
		priceMonth = 0.0f;
		pricesMonth[0] = 0.0f;
		pricesMonth[1] = 0.0f;
		tempInf = 0.0f;
		month = 1;
	}
	public override void CollectObservations(VectorSensor sensor)
	{
		sensor.AddObservation(speedMove);
		sensor.AddObservation(timeMining);
		sensor.AddObservation(amountGold);
		sensor.AddObservation(pickaxeСost);
		sensor.AddObservation(profitPercentage);
	}

	public override void OnActionReceived(ActionBuffers actionBuffers)
	{
		if (month < 3 || setSensor == true)
		{
			speedMove = Mathf.Clamp(actionBuffers.ContinuousActions[0]*10, 1f, 10f);
			// Debug.Log("SpeedMove: " + speedMove);
			timeMining = Mathf.Clamp(actionBuffers.ContinuousActions[1]*10, 1f, 10f);
			// Debug.Log("timeMining: " + timeMining);
			setSensor = false;
			if (checkStartMonth == false)
			{
				Debug.Log("Start Coroutine StartMonth");
				StartCoroutine(StartMonth());
			}

			if (transform.position != goldMine.transform.position & checkMiningFinish == false)
			{
				transform.position = Vector3.MoveTowards(transform.position, goldMine.transform.position, Time.deltaTime * speedMove);
			}

			if (transform.position == goldMine.transform.position & checkMiningStart == false)
			{
				Debug.Log("Start Coroutine StartGoldMine");
				StartCoroutine(StartGoldMine());
			}

			if (transform.position != village.transform.position & checkMiningFinish == true)
			{
				transform.position = Vector3.MoveTowards(transform.position, village.transform.position, Time.deltaTime * speedMove);
			}

			if (transform.position == village.transform.position & checkMiningStart == true)
			{
				checkMiningFinish = false;
				checkMiningStart = false;
				setSensor = true;
				amountGold = Mathf.Clamp(actionBuffers.ContinuousActions[2] * 10, 1f, 10f);
				Debug.Log("amountGold: " + amountGold);
				pickaxeСost = Mathf.Clamp(actionBuffers.ContinuousActions[3] * 100, 100f, 1000f);
				Debug.Log("pickaxeСost: " + pickaxeСost);
				profitPercentage = Mathf.Clamp(MathF.Abs(actionBuffers.ContinuousActions[4]), 0.1f, 0.5f);
				Debug.Log("profitPercentage: " + profitPercentage);

				if (month != 2)
				{
					priceMonth = pricesMonth[0] + ((pickaxeСost + pickaxeСost * profitPercentage) / amountGold);
					pricesMonth[0] = priceMonth;
					Debug.Log("priceMonth: " + priceMonth);
				}
				if (month == 2)
				{
					priceMonth = pricesMonth[1] + ((pickaxeСost + pickaxeСost * profitPercentage) / amountGold);
					pricesMonth[1] = priceMonth;
					Debug.Log("priceMonth: " + priceMonth);
				}

			}
		}
		else
		{
			tempInf = ((pricesMonth[1] - pricesMonth[0]) / pricesMonth[0]) * 100;
			if (tempInf is <= 6f)
			{
				SetReward(1.0f);
				Debug.Log("True");
				Debug.Log("tempInf: " + tempInf);
				EndEpisode();
			}
			else
			{
				SetReward(-1.0f);
				Debug.Log("False");
				Debug.Log("tempInf: " + tempInf);
				EndEpisode();
			}
		}
	}

	IEnumerator StartGoldMine()
	{
		checkMiningStart = true;
		yield return new WaitForSeconds(timeMining);
		Debug.Log("Mining Finish");
		checkMiningFinish = true;
	}

	IEnumerator StartMonth()
	{
		checkStartMonth = true;
		yield return new WaitForSeconds(60);
		checkStartMonth = false;
		month++;
		//Debug.Log(month);
		//Debug.Log(string.Join(" ", new List<float>(pricesMonth).ConvertAll(i => i.ToString()).ToArray()));
	}
}
```

## Задание 1
### Найдите внутри C# скрипта “коэффициент корреляции ” и сделать выводы о том, как он влияет на обучение модели.
Ход работы:
- Скачать дистрибутив анаконды с сайта anaconda.com
- Открыть Anaconda.Navigator
- Открыть Jupyter Notebook
- Создать новый файл HelloWorld.ipynb
- Написать в файле

- Запускаем

![image](https://github.com/Yrwlcm/DA-in-GameDev-lab1/assets/99079920/a1c1a877-7cdd-4e8a-87f7-ab5707ada16f)

## Задание 2
### Написать программу Hello World на C# с запуском на Unity. 

Ход работы:
- Скачать установщик Unity с сайта unity.com
- Регестрируем аккаунт
- Создаем новый пустой проект
- Создаем новый обьект Ui -> Text - TextMashPro с текстом Hello World
![image](https://github.com/Yrwlcm/DA-in-GameDev-lab1/assets/99079920/31c16656-27eb-47c7-82a9-d2787ea33ea4)
- Создаем новый скрипт с названием HelloWorldScript
- Внутрь скрипта пишем
``` C#
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelloWorldScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Hello world!");
    }
}
```
- Привязываем его к обьекту Canvas
![image](https://github.com/Yrwlcm/DA-in-GameDev-lab1/assets/99079920/99f4ad08-3f28-4b35-8e32-0d6b4593fd57)

- Теперь при запуске программы на экране будет надпись Hello World и в консоль будет выведено Hello World
![image](https://github.com/Yrwlcm/DA-in-GameDev-lab1/assets/99079920/6e320b91-e4c3-4716-8e3e-6de24ca6a482)

## Задание 3
### Оформить отчет в виде документации на github (markdown-разметка).

- Открыть репозиторий с лабораторной работой
- Создать его клон с помощью Fork
- Оформить отчет

## Выводы

В ходе этой лабораторной работой, я установил Anaconda и Unity на свой ПК и познакомился с основами работы с ними.

| Plugin | README |
| ------ | ------ |
| Dropbox | [plugins/dropbox/README.md][PlDb] |
| GitHub | [plugins/github/README.md][PlGh] |
| Google Drive | [plugins/googledrive/README.md][PlGd] |
| OneDrive | [plugins/onedrive/README.md][PlOd] |
| Medium | [plugins/medium/README.md][PlMe] |
| Google Analytics | [plugins/googleanalytics/README.md][PlGa] |

## Powered by

**BigDigital Team: Denisov | Fadeev | Panov**
