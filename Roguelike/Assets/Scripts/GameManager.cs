using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public float turnDelay = 0.1f;                          //Delay between each Player turn.
    public int healthPoints = 100;                          //Starting value for Player health points.
    public static GameManager instance = null;              //Static instance of GameManager which allows it to be accessed by any other script.
    [HideInInspector] public bool playersTurn = true;        //Boolean to check if it's players turn, hidden in inspector but public.

    // Chapter 7 - make the enemies faster - adaptive difficulty
    public bool enemiesFaster = false;
	public bool enemiesSmarter = false;
	public int enemySpawnRatio = 20;                        // represented as 1/enemySpawnRatio ie. 1/20

    //CHAPTER 3
    private BoardManager boardScript;                       //Store a reference to our BoardManager which will set up the level.
    private DungeonManager dungeonScript;
	private Player playerScript;
    private List<Enemy> enemies;                            //List of all Enemy units, used to issue them move commands.
    private bool enemiesMoving;                             //Boolean to check if enemies are moving.

    // Chapter 7 - adding a flag to determine if the player is in the dungeon
    private bool playerInDungeon;

    //Awake is always called before any Start functions
    void Awake() {
        //Check if instance already exists
        if (instance == null)
            //if not, set instance to this
            instance = this;
        //If instance already exists and it's not this:
        else if (instance != this)
            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);

        //Sets this to not be destroyed when reloading scene
        DontDestroyOnLoad(gameObject);

        //Assign enemies to a new List of Enemy objects.
        enemies = new List<Enemy>();
		enemiesFaster = false;
		enemiesSmarter = false;

        //CHAPTER 3
        //Get a component reference to the attached BoardManager script
        boardScript = GetComponent<BoardManager> ();

		dungeonScript = GetComponent<DungeonManager> ();
		playerScript = GameObject.FindGameObjectWithTag ("Player").GetComponent<Player> ();

        //Call the InitGame function to initialize the first level 
        InitGame();
	}

    //This is called each time a scene is loaded.
    void OnLevelWasLoaded(int index) {
        //Call InitGame to initialize our level.
        InitGame();
	}

    //Initializes the game for each level.
    void InitGame() {
        //Clear any Enemy objects in our List to prepare for next level.
        enemies.Clear();
        
        //Call the SetupScene function of the BoardManager script, pass it current level number.
        boardScript.BoardSetup();
		
        // Chapter 7 - initialize the player ouside of a dungeon
		playerInDungeon = false;
	}

    //Update is called every frame.
    void Update() {
        //Check that playersTurn or enemiesMoving or doingSetup are not currently true.
        if (playersTurn || enemiesMoving)
            //If any of these are true, return and do not start MoveEnemies.
            return;

        //Start moving enemies.
        StartCoroutine(MoveEnemies ());
	}

	// CHAPTER 8 - ADDED MUSIC TO ENEMY ENCOUNTERS
	// Chapter7 - adding to an enemy list to cycle threw movement
	//Call this to add the passed in Enemy to the List of Enemy objects.
	public void AddEnemyToList(Enemy script)
	{
		//Add Enemy to List enemies.
		enemies.Add(script);
		SoundManager.instance.FormAudio (true);
	}
	// Chapter 7 - call this when an enemy is destroyed
	public void RemoveEnemyFromList(Enemy script)
	{
		//Remove Enemy from List enemies.
		enemies.Remove(script);
		if (enemies.Count == 0) {
			SoundManager.instance.FormAudio (false);
		}
	}

	public void GameOver() {
		enabled = false;
	}

	// Chapter 7 - adding enemy movement turns, should only work when there are enemies on screen and in the list to move
	IEnumerator MoveEnemies() {
		//While enemiesMoving is true player is unable to move.
		enemiesMoving = true;
		
		//Wait for turnDelay seconds, defaults to .1 (100 ms).
		yield return new WaitForSeconds(turnDelay);
		
		//If there are no enemies spawned (IE in first level):
		if (enemies.Count == 0) 
		{
			//Wait for turnDelay seconds between moves, replaces delay caused by enemies moving when there are none.
			yield return new WaitForSeconds(turnDelay);
		}
		List<Enemy> enemiesToDestroy = new List<Enemy>();
		//Loop through List of Enemy objects.
		for (int i = 0; i < enemies.Count; i++)
		{
			// Chapter 7 - logic for switching between dungeon enemies and world enemies
			if (playerInDungeon) {
				if ((!enemies[i].getSpriteRenderer().isVisible)) {
					if (i == enemies.Count - 1)
						yield return new WaitForSeconds(enemies[i].moveTime); // This waits for all of them...
					continue;
				}
			} else {
			// Chapter 7 - if enemy moves off screen then we want them to be gone forever
				if ((!enemies[i].getSpriteRenderer().isVisible) || (!boardScript.checkValidTile (enemies[i].transform.position))) {
					enemiesToDestroy.Add(enemies[i]);
					continue;
				}
			}

			//Call the MoveEnemy function of Enemy at index i in the enemies List.
			enemies[i].MoveEnemy ();
			
			//Wait for Enemy's moveTime before moving next Enemy, 
			yield return new WaitForSeconds(enemies[i].moveTime);
		}
		//Once Enemies are done moving, set playersTurn to true so player can move.
		playersTurn = true;
		
		//Enemies are done moving, set enemiesMoving to false.
		enemiesMoving = false;

		// Chapter 7 - remove the dead enemies
		for (int i = 0; i < enemiesToDestroy.Count; i++) {
			//enemies.Remove(enemiesToDestroy[i]); TODO this is an error in chapter 7!
			RemoveEnemyFromList(enemiesToDestroy[i]);
			Destroy(enemiesToDestroy[i].gameObject);
		}
		enemiesToDestroy.Clear ();
	}

	public void updateBoard (int horizantal, int vertical) {
		boardScript.addToBoard(horizantal, vertical);
	}

	public void enterDungeon () {
		dungeonScript.StartDungeon ();
		boardScript.SetDungeonBoard (dungeonScript.gridPositions, dungeonScript.maxBound, dungeonScript.endPos);
		playerScript.dungeonTransition = false;
		// Chapter 7 - set flag for player inside a dungeon
		playerInDungeon = true;

		// Chapter 7 - Clear all enemies from the world map when you enter a dungeon
		for (int i = 0; i < enemies.Count; i++) {
			Destroy(enemies[i].gameObject);
		}
		enemies.Clear ();
	}

	// CHAPTER 8 - END MUSIC WHEN YOU LEAVE DUNGEON
	public void exitDungeon () {
		boardScript.SetWorldBoard ();
		playerScript.dungeonTransition = false;
		// Chapter 7 - set flag for player outside a dungeon
		playerInDungeon = false;
		// Chapter 7 - clear the enemy lists
		enemies.Clear ();
		SoundManager.instance.FormAudio (false);
	}
}