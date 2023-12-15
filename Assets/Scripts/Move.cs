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
			if (tempInf <= 6f)
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
