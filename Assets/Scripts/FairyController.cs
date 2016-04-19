﻿using UnityEngine;
using System.Collections;

public enum FairyEmotion {
	Happy = 1,
	Worried = 2
}

public class FairyController : BaseClass {
	
	enum FairyState {
		None = 0,
		Arrive = 1,
		Leave = 2,
		Hover = 3
	}

	//Components
	Animation fairyAnimation;

	public ParticleSystem appearFx;
	public AudioClip appearSound;

	public ParticleSystem fairyDustFx;
	public AudioClip fairyDustSound;

	public ParticleSystem fairySpellFx;
	public AudioClip fairySpellSound;

	Transform player;
	PlayerController playerController;

	FairyState fairyState = FairyState.None;


	// The distance in the x-z plane to the target
	const float DEFAULT_DISTANCE = 0.7f;
	float distance = DEFAULT_DISTANCE;

	// the height we want the fairy to be above the player
	public const float DEFAULT_HEIGHT = 2.1f;
	float height = DEFAULT_HEIGHT;

	//Where to position the fairt relative to the player when appearing next to player
	Vector3 fairyRelativePos = new Vector3(-0.6f , DEFAULT_HEIGHT , DEFAULT_DISTANCE );

	// How much we 
	const float DEFAULT_HEIGHT_DAMPING = 6f;
	float heightDamping = DEFAULT_HEIGHT_DAMPING;

	const float DEFAULT_ROTATION_DAMPING = 3f;
	float rotationDamping = DEFAULT_ROTATION_DAMPING;

	const float DEFAULT_Y_ROTATION_OFFSET = 168f;
	float yRotationOffset = DEFAULT_Y_ROTATION_OFFSET;

	const float DEFAULT_X_ROTATION = 9f;
	float xRotation = DEFAULT_X_ROTATION;

	Vector3 xOffset = new Vector3( 0.6f, 0, 0 );

	const float FAIRY_HEIGHT_ABOVE_GROUND = 1.05f;  //On Level Start

	void Awake()
	{
		//Get a copy of the components
		fairyAnimation = (Animation) GetComponent("Animation");
		fairyAnimation["Revive"].speed = 1.2f;
		player = GameObject.FindGameObjectWithTag("Player").transform;
		playerController = (PlayerController) player.gameObject.GetComponent(typeof(PlayerController));
	}

	void Start()
	{
		//Adjust the fairy's Y position depending on the height of the ground below her.
		RaycastHit hit;
		if (Physics.Raycast(new Vector3(0,10f,0), Vector3.down, out hit, 15.0F ))
		{
			transform.position = new Vector3( transform.position.x, hit.point.y + FAIRY_HEIGHT_ABOVE_GROUND, transform.position.z );
		}
	}

	// Update is called once per frame
	void LateUpdate ()
	{
		if( ( GameManager.Instance.getGameState() == GameState.Normal || GameManager.Instance.getGameState() == GameState.Checkpoint ) && fairyState == FairyState.Hover && playerController.getCharacterState() != CharacterState.Dying )
		{
			positionFairy ();
		}
	}

	public void setYRotationOffset( float offset )
	{
		yRotationOffset = offset;
	}

	public void resetYRotationOffset()
	{
		yRotationOffset = DEFAULT_Y_ROTATION_OFFSET;
	}

	private void positionFairy ()
	{
		// Calculate the current rotation angles
		float wantedRotationAngle = player.eulerAngles.y + yRotationOffset;
		float wantedHeight = player.position.y + height;
		
		float currentRotationAngle = transform.eulerAngles.y;
		float currentHeight = transform.position.y;
		
		// Damp the rotation around the y-axis
		currentRotationAngle = Mathf.LerpAngle (currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);
		
		// Damp the height
		currentHeight = Mathf.Lerp (currentHeight, wantedHeight, heightDamping * Time.deltaTime);
		
		// Convert the angle into a rotation
		Quaternion currentRotation = Quaternion.Euler (0, currentRotationAngle, 0);
		
		//Order of rotations is ZXY

		// Set the position of the fairy on the x-z plane to:
		// distance meters behind the target
		transform.position = player.position;
		transform.position -= currentRotation * Vector3.forward * distance;
		
		// Set the height of the fairy
		transform.position = new Vector3( transform.position.x, currentHeight, transform.position.z );
		
		// Always look at the target
		transform.LookAt (player);
		
		//Tilt the camera down
		transform.rotation = Quaternion.Euler( xRotation, transform.eulerAngles.y, transform.eulerAngles.z );

		//More fairy slightly to the left
		Vector3 exactPos = transform.TransformPoint( xOffset );
		transform.position = exactPos;
	}

	public void Arrive( float timeToArrive )
	{
		fairyState = FairyState.Arrive;
		Vector3 arrivalStartPos = new Vector3( -18f, 12f, PlayerController.getPlayerSpeed() * 2f );
		Vector3 exactPos = player.TransformPoint(arrivalStartPos);
		transform.position = exactPos;
		transform.rotation = Quaternion.Euler( 0, player.transform.eulerAngles.y + 90f, transform.eulerAngles.z );
		StartCoroutine("MoveToPosition", timeToArrive );
	}

	public void Appear( FairyEmotion fairyEmotion )
	{
		transform.localScale = new Vector3( 1f, 1f, 1f );
		positionFairy ();
		if( fairyEmotion == FairyEmotion.Happy )
		{
			fairyAnimation.Play("Hover_Happy");
		}
		else
		{
			fairyAnimation.Play("Hover_Worried");
		}
		appearFx.Play();
		GetComponent<AudioSource>().PlayOneShot( appearSound );
		fairyState = FairyState.Hover;
	}

	public void Disappear()
	{
		GetComponent<AudioSource>().PlayOneShot( appearSound );
		transform.localScale = new Vector3( 0.002f, 0.002f, 0.002f );
		appearFx.Play();
		Invoke("Disappear_part2", 2.3f);
	}

	public void Disappear_part2()
	{
		fairyState = FairyState.None;
	}

	public void CastSpell()
	{
		fairyAnimation.CrossFade("CastSpell", 0.2f);
		fairyAnimation.PlayQueued("Hover_Happy");
		Invoke ("playCastSpellFx", 4f );
	}

	void playCastSpellFx()
	{
		fairySpellFx.Play();
		GetComponent<AudioSource>().PlayOneShot( fairySpellSound );
	}

	private IEnumerator MoveToPosition( float timeToArrive )
	{
		//Step 1 - Take position in front of player
		float startTime = Time.time;
		float elapsedTime = 0;
		float startYrot = transform.eulerAngles.y;
		Vector3 startPosition = transform.position;
		
		while ( elapsedTime <= timeToArrive )
		{
			elapsedTime = Time.time - startTime;
			
			//Percentage of time completed 
			float fracJourney = elapsedTime / timeToArrive;
			
			float yRot = Mathf.LerpAngle( startYrot, player.eulerAngles.y + 180f, fracJourney );
			transform.eulerAngles = new Vector3 ( transform.eulerAngles.x, yRot, transform.eulerAngles.z );
			
			Vector3 exactPos = player.TransformPoint(fairyRelativePos);
			transform.position = Vector3.Lerp( startPosition, exactPos, fracJourney );
			
			//Tilt the fairy down
			transform.rotation = Quaternion.Euler( -8f, transform.eulerAngles.y, transform.eulerAngles.z );
			
			yield return _sync();  
			
		}
		fairyState = FairyState.Hover;
	}

	public void revivePlayer ()
	{
		//Note: the revive animation plays at 1.2 the speed
		transform.localScale = new Vector3( 1f, 1f, 1f );

		//Move Fairy to player body and play a sprinkle animation
		float fairyRotY = player.eulerAngles.y + 205f;
		Vector3 relativePos = new Vector3(0.3f , 0.5f , 1f );
		Vector3 exactPos = player.TransformPoint(relativePos);
		transform.position = new Vector3( exactPos.x, exactPos.y, exactPos.z );
		transform.rotation = Quaternion.Euler( 0, fairyRotY, 0 );
		fairyAnimation.Play("Revive");
		fairyAnimation.PlayQueued("Hover_Happy", QueueMode.CompleteOthers);
		Invoke("sprinkleFairyDustStart", 1.64f );
		Invoke("continueResurection", 4.16f ); //start get up at around frame 285 of the revive animation
	}	

	void sprinkleFairyDustStart()
	{
		GetComponent<AudioSource>().PlayOneShot( fairyDustSound );
		fairyDustFx.Play ();
		Invoke("sprinkleFairyDustStop", 0.96f );
	}

	void sprinkleFairyDustStop()
	{
		fairyDustFx.Stop ();
	}

	private void continueResurection()
	{
		playerController.resurrectMiddle();
	}

	private void playAnimation( string animationName, WrapMode mode )
	{
		fairyAnimation[ animationName ].wrapMode = mode;
		fairyAnimation[ animationName ].speed = 1f;
		fairyAnimation.CrossFade(animationName, 0.1f);
	}
}
