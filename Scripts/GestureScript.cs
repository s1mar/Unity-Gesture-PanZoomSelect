using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureScript : MonoBehaviour {

	GameObject selectedGameObject;
	public float PAN_SPEED = 0.3f;
	float touchTime;
	public float SCALE_DURATION = 2.0f;
	bool rotationWidgetSelected = false;
	bool panThroughLock = false;

	private float scale_factor= 0.07f;
	private float MAXSCALE = 6.0f, MIN_SCALE = 0.6f; // zoom-in and zoom-out limits
	private Vector2 prevDist = new Vector2(0,0);
	private Vector2 curDist = new Vector2(0,0);
	private Vector2 midPoint = new Vector2(0,0);

	enum Action{

		ACTION_SELECT,
		ACTION_DESELECT,
		ACTION_PAN_ACROSS,
		ACTION_PAN_THROUGH,
		ACTION_SCALE,
		ACTION_ROTATE,
		ACTION_NONE
	}

	Action prevAction = Action.ACTION_NONE;

	TouchPhase lastTouchPhase;

	Color _lastTint;
	void rotationEnabled(){

		if (Input.touchCount == 1) {
			Touch touch = Input.GetTouch (0);
			switch (touch.phase) {

			case TouchPhase.Began:

				lastTouchPhase = TouchPhase.Began;

				selectObject (touch);
				return;
			case TouchPhase.Moved:

				lastTouchPhase = TouchPhase.Moved;
				rotateAcrossYusingX (touch);
				return;
			case TouchPhase.Ended:
				if (lastTouchPhase == TouchPhase.Moved) {
					deselectGameObject ();
				}

				lastTouchPhase = TouchPhase.Moved;
				return;
			}



		}
	}

	void rotateAcrossYusingX(Touch touch){
		prevAction = Action.ACTION_ROTATE;
		Vector3 deltaShift = touch.deltaPosition;
		Vector3 translationVector = new Vector3 (0, -deltaShift.x * PAN_SPEED * 3, 0);
		selectedGameObject.transform.parent.transform.Rotate(translationVector,Space.Self);
	}
		
	// Update is called once per frame
	void Update () {

		int touchCount = Input.touchCount; 
		Touch[] touches = Input.touches;
		Touch touchFirst = touches [0];


		if (rotationWidgetSelected) {
			rotationEnabled ();
			return;
		}

		if (touchCount == 1) {
		
			switch (touchFirst.phase) {

			case TouchPhase.Began:
				
				lastTouchPhase = TouchPhase.Began;
				selectObject (touchFirst);
				return;
			case TouchPhase.Moved:
				
				lastTouchPhase = TouchPhase.Moved;
				panAcross (touchFirst);
				//panAcross();
				return;

			case TouchPhase.Ended:
				if (lastTouchPhase == TouchPhase.Moved) {
					deselectGameObject ();
				}
			
				lastTouchPhase = TouchPhase.Moved;
				return;
			}
		
		} else if (touchCount == 2) {
			
			switch (touchFirst.phase) {

			case TouchPhase.Moved:
				if (selectedGameObject == null || touches [1].phase != TouchPhase.Moved) {
					return;
				}

				if (panThroughLock)
					return;
				scaleAddendum();

				return;
			}
				
		}
		else if(touchCount == 3){
			if (touches [0].phase == TouchPhase.Began && touches [1].phase == TouchPhase.Began && touches [2].phase == TouchPhase.Began) {
				panThroughLock = true;
			
			}
			else if (touches [0].phase == TouchPhase.Moved && touches [1].phase == TouchPhase.Moved && touches [2].phase == TouchPhase.Moved) {
				panThroughLock = true;
				panThrough (touches [0]);
			} else if(touches [0].phase == TouchPhase.Ended && touches [1].phase == TouchPhase.Ended && touches [2].phase == TouchPhase.Ended){
			
				panThroughLock = false;
			}

		}


	}
	void selectObject(Touch touch){

		RaycastHit hitObject;
		Ray ray = Camera.main.ScreenPointToRay (touch.position);
		if (Physics.Raycast (ray, out hitObject)) {
			if (hitObject.collider.tag.Equals ("target")) {
						//switch object script
				rotationWidgetSelected = false;
				if (hitObject.collider.gameObject == selectedGameObject && prevAction == Action.ACTION_SELECT) {
					prevAction = Action.ACTION_NONE;
					return; //do nothing
				}
				switchSelectGameObject(hitObject.collider.gameObject);	
			}else if(hitObject.collider.tag.Equals ("rotation")){
				rotationWidgetSelected = true;
				switchSelectGameObject(hitObject.collider.gameObject);	
			}
		
		}

		}

	void switchSelectGameObject(GameObject obj){
		if (obj == selectedGameObject) {
		//deselect
			switchGlow(false,obj);
			selectedGameObject= null;
			prevAction = Action.ACTION_DESELECT;
			return;
		} 
		switchGlow (false, selectedGameObject);
		selectedGameObject = obj;
		prevAction = Action.ACTION_SELECT;
		switchGlow (true, selectedGameObject);
	}

	void deselectGameObject(){
		switchGlow (false, selectedGameObject);
		selectedGameObject = null;
		prevAction = Action.ACTION_DESELECT;
	}

	void panAcross(Touch touch){
		if (selectedGameObject == null) {
			return;
		}
		prevAction = Action.ACTION_PAN_ACROSS;	
		Vector2 deltaShift = touch.deltaPosition;
		Vector3 translationVector = new Vector3 (deltaShift.x * PAN_SPEED * Time.deltaTime, deltaShift.y * PAN_SPEED * Time.deltaTime, 0);
		selectedGameObject.transform.Translate (translationVector,Space.World);
	}

	void panThrough(Touch touch){
		prevAction = Action.ACTION_PAN_THROUGH;	
		Vector2 newPos = touch.position;
		Vector2 oldPos = newPos - touch.deltaPosition;
		float diffMagnitude = touch.deltaPosition.magnitude;


		if (newPos.magnitude > oldPos.magnitude) {
			//MOVE CLOSE
			Vector3 translation = new Vector3(0,0,PAN_SPEED*Time.deltaTime*-diffMagnitude);
			selectedGameObject.transform.Translate(translation,Space.World);

		} else {
			//MOVE FAR
			Vector3 translation = new Vector3(0,0,PAN_SPEED*Time.deltaTime*diffMagnitude);
			selectedGameObject.transform.Translate(translation,Space.World);
		}


	}
		

	void switchGlow(bool flip,GameObject obj){
		if (obj == null || obj.tag.Equals("rotation")) {
			return;
		}


		if (!flip) {

			obj.GetComponent<GLOW>().glowOFF();
		
		} else {

			obj.GetComponent<GLOW>().glowON();
		}

	}
		
	private void scaleFromPosition(Vector3 scale, Vector3 fromPos)
	{	
		selectedGameObject.transform.localScale = scale;
	
	}

	private void scaleAddendum(){
		prevAction = Action.ACTION_SCALE;
		midPoint = new Vector2(((Input.GetTouch(0).position.x + Input.GetTouch(1).position.x)/2), ((Input.GetTouch(0).position.y + Input.GetTouch(1).position.y)/2));
		midPoint = Camera.main.ScreenToWorldPoint(midPoint);
		curDist = Input.GetTouch(0).position - Input.GetTouch(1).position; //current distance between finger touches
		prevDist = ((Input.GetTouch(0).position - Input.GetTouch(0).deltaPosition) - (Input.GetTouch(1).position - Input.GetTouch(1).deltaPosition)); //difference in previous locations using delta positions
		float touchDelta = curDist.magnitude - prevDist.magnitude;
		if(touchDelta>0)
		{
			if(selectedGameObject.transform.localScale.x < MAXSCALE && selectedGameObject.transform.localScale.y < MAXSCALE && selectedGameObject.transform.localScale.z < MAXSCALE)
			{
				Vector3 scale = new Vector3(selectedGameObject.transform.localScale.x + scale_factor, selectedGameObject.transform.localScale.y + scale_factor, selectedGameObject.transform.localScale.z + scale_factor);
				scale.x = (scale.x > MAXSCALE) ? MAXSCALE : scale.x;
				scale.y = (scale.y > MAXSCALE) ? MAXSCALE : scale.y;
				scale.z = (scale.z > MAXSCALE) ? MAXSCALE : scale.z;
				scaleFromPosition(scale,midPoint);
			}
		}
		//Zoom in
		else if(touchDelta<0)
		{
			if(selectedGameObject.transform.localScale.x > MIN_SCALE && selectedGameObject.transform.localScale.y > MIN_SCALE && selectedGameObject.transform.localScale.z > MIN_SCALE)
			{
				Vector3 scale = new Vector3(selectedGameObject.transform.localScale.x + scale_factor*-1, selectedGameObject.transform.localScale.y + scale_factor*-1, selectedGameObject.transform.localScale.z + scale_factor*-1);
				scale.x = (scale.x < MIN_SCALE) ? MIN_SCALE : scale.x;
				scale.y = (scale.y < MIN_SCALE) ? MIN_SCALE : scale.y;
				scale.z = (scale.z < MIN_SCALE) ? MIN_SCALE : scale.z;
				scaleFromPosition(scale,midPoint);
			}
		}

	}
}



