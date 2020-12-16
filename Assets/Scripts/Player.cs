using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;

public class Player : MonoBehaviour
{
    [Header("Setup")]
    public bool DEBUG = false;
    public CharacterController characterController;
    public AnimationController animationController;
    public GameObject colliders;

    private string username;
    private int id;

    [Header("Movement")]
    public float gravity = -9.81f;
    public float moveSpeed = 5f;
    public float jumpSpeed = 5f;

    private float yVelocity;

    [Header("Health")]
    public float maxHealth = 100.0f;
    public float respawnTime = 5f;

    private float health;

    [Header("Projectile")]
    public float throwForce = 600f;
    public int itemAmount = 0;
    public int maxItemAmount = 3;

    [Header("Gun")]
    public Gun gun;
    public Transform shootOrigin;

    [Header("Time Sync")]
    public SyncedTime syncedTime;

    //Score
    private Score score = new Score();
    // Input/ticks history
    private float saveTime = 1f; //time in seconds after inputs and ticks will be discarded
    private ThreadSafeList<UserInput> unprocessedUserInput = new ThreadSafeList<UserInput>(new List<UserInput>());
    private List<UserInput> processedUserInput = new List<UserInput>();
    private List<PlayerState> ticks = new List<PlayerState>();

    private void Start()
    {
        if (DEBUG) Debug.Log("Fixed Delta:" + Time.fixedDeltaTime);
    }

    public int GetID()
    {
        return id;
    }
    public float GetHealth()
    {
        return health;
    }

    public string GetUsername()
    {
        return username;
    }

    public void AddDamage(float damage)
    {
        score.damageDone += damage;
    }

    public void AddKills(int _kills)
    {
        score.kills += _kills;
    }

    public void UpdateSyncTime(int packetId, float clientTime)
    {
        syncedTime.UpdateTime(packetId, clientTime);
    }

    /// <summary>
    /// Set id, username and spawn position
    /// </summary>
    /// <param name="_id"></param>
    /// <param name="_username"></param>
    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
        health = maxHealth;
        yVelocity = 0f;
        SetSpawnPosition();
    }

    /// <summary>
    /// Set new random spawn position
    /// </summary>
    private void SetSpawnPosition()
    {
        SpawnPoint sp = SpawnPoints.GetRandomSpawnPoint();
        transform.position = sp.Position;
        transform.rotation = sp.Rotation;
    }

    /// <summary>
    /// Initialize input/ticks
    /// </summary>
    private void InitInputAndTicks()
    {
        for (float x = saveTime; x > 0.0f; x -= Time.fixedDeltaTime)
        {
            processedUserInput.Add(new UserInput { Inputs = new bool[5], Time = syncedTime.GetClientTime() - x, Rotation = Quaternion.identity });
            unprocessedUserInput.Add(new UserInput { Inputs = new bool[5], Time = syncedTime.GetClientTime() - x, Rotation = Quaternion.identity });
            ticks.Add(new PlayerState { Position = transform.position, Rotation = transform.rotation, Time = syncedTime.GetClientTime() - x, YVelocity = yVelocity, AnimationTime = 0.0f });
        }
    }

    public void FixedUpdate()
    {
        //Is time synced?
        syncedTime.SendTimePacket(id);
        if (!syncedTime.isReady) return;
        //Initialize ticks
        if (ticks.Count == 0) InitInputAndTicks();
        //Get client time
        float currentTime = syncedTime.GetClientTime();
        //Clone multithreaded lists
        List<UserInput> tempUnprocessedUserInput = unprocessedUserInput.Clone();
        tempUnprocessedUserInput.RemoveAll(u => u.Time < ticks.First().Time || u.Time < currentTime - saveTime);
        unprocessedUserInput = new ThreadSafeList<UserInput>(tempUnprocessedUserInput);
        List<UserInput> uInput = unprocessedUserInput.Clone();
        //Is Alive
        if (health > 0f)
        {
            //Has new client input
            if (uInput.Count > 0)
            {
                Vector2 _inputDirection = Vector2.zero;
                uInput = uInput.OrderBy(x => x.Time).ToList();
                //Merge processed + unprocessed inputs
                List<UserInput> allInputs = new List<UserInput>();
                allInputs.AddRange(processedUserInput);
                allInputs.AddRange(uInput);

                //Server Reconciliation
                //Create new ticks
                if (uInput.First().Time < ticks.Last().Time) 
                {
                    //Rewind ticks till oldest unprocessedUserInput
                    ticks.RemoveAll(t => t.Time > uInput.First().Time);
                    //Set last valid tick information
                    characterController.enabled = false;
                    characterController.transform.position = ticks.Last().Position;
                    characterController.transform.rotation = ticks.Last().Rotation;
                    yVelocity = ticks.Last().YVelocity;
                    characterController.enabled = true;
                    characterController.Move(Vector3.zero); //Reset isGrounded
                    if (ticks.Count > 2) animationController.RewindAnimation(ticks.Last().AnimationTime, ticks.Last().Position - ticks[ticks.Count - 2].Position, transform.forward);

                    float newTickTime;
                    do
                    {
                        float lastTickTime = ticks.Last().Time;
                        newTickTime = lastTickTime + Time.fixedDeltaTime;
                        if (newTickTime > currentTime) { newTickTime = currentTime; }
                        List<UserInput> movesThisTick = allInputs.Where(a => a.Time > lastTickTime && a.Time <= newTickTime).OrderBy(ui => ui.Time).ToList();
                        UserInput prevTickMove = allInputs.Where(a => a.Time <= lastTickTime).OrderByDescending(x => x.Time).FirstOrDefault();
                        if (prevTickMove != default && prevTickMove != null)
                        {
                            prevTickMove.Time = ticks.Last().Time;
                            movesThisTick.Insert(0, prevTickMove);
                        }
                        if (movesThisTick.Count > 0)
                        {
                            //Apply tick inputs
                            for (int i = 0; i < movesThisTick.Count; i++)
                            {
                                float moveTime;
                                if (i == movesThisTick.Count - 1)
                                {
                                    moveTime = newTickTime - movesThisTick[i].Time;
                                }
                                else
                                {
                                    moveTime = movesThisTick[i + 1].Time - movesThisTick[i].Time;
                                }
                                Move(movesThisTick[i].Inputs, movesThisTick[i].Rotation, moveTime);
                            }
                        }
                        ticks.Add(new PlayerState { Position = transform.position, Rotation = transform.rotation, Time = newTickTime, YVelocity = yVelocity, AnimationTime = animationController.GetNormalizedTime() });
                    } while (newTickTime != currentTime);
                }
                //Create new tick
                else
                {
                    //Execute last ProcessedInput from last tick till first UnProcessedInput
                    float moveTime = uInput.First().Time - ticks.Last().Time;
                    Move(processedUserInput.Last().Inputs, processedUserInput.Last().Rotation, moveTime);
                    //Execute unprocessed Input
                    for (int i = 0; i < uInput.Count; i++)
                    {
                        if (i == uInput.Count - 1)
                        {
                            moveTime = currentTime - uInput[i].Time;
                        }
                        else
                        {
                            moveTime = uInput[i + 1].Time - uInput[i].Time;
                        }
                        Move(uInput[i].Inputs, uInput[i].Rotation, moveTime);
                    }
                    ticks.Add(new PlayerState { Position = transform.position, Rotation = transform.rotation, Time = currentTime, YVelocity = yVelocity, AnimationTime = animationController.GetNormalizedTime() });
                }
            }
            //No new unprocessed input
            //Execute last processed input 
            //Create server tick
            else
            {
                Move(processedUserInput.Last().Inputs, processedUserInput.Last().Rotation, currentTime - ticks.Last().Time);
                ticks.Add(new PlayerState { Position = transform.position, Rotation = transform.rotation, Time = currentTime, YVelocity = yVelocity, AnimationTime = animationController.GetNormalizedTime() });
            }
        }
        //Is Dead
        else
        {
            yVelocity = 0;
            ticks.Add(new PlayerState { Position = transform.position, Rotation = transform.rotation, Time = currentTime, YVelocity = yVelocity, AnimationTime = animationController.GetNormalizedTime() });
        }

        //move from unprocessed to processed
        foreach (UserInput u in uInput)
        {
            unprocessedUserInput.Remove(u);
            processedUserInput.Add(u);
        }
        //remove old input/ticks
        if (ticks.Count > saveTime / Time.fixedDeltaTime) ticks.RemoveAt(0);
        while (processedUserInput.Count > saveTime / Time.fixedDeltaTime) processedUserInput.RemoveAt(0);
        processedUserInput = processedUserInput.OrderBy(pui => pui.Time).ToList();
        //send new tick to all players
        ServerSend.PlayerData(id, ticks.Last(), (int)score.damageDone, score.kills, score.deaths);

        Logs();
    }

    /// <summary>
    /// Debug logs
    /// </summary>
    private void Logs()
    {
        if (!DEBUG) return;
        Debug.Log("pInput Count: " + processedUserInput.Count);
        Debug.Log("uInput Count: " + unprocessedUserInput.Count);
        Debug.Log("ticks Count: " + ticks.Count);
    }

    /// <summary>
    /// Apply keyboard input and mouse rotation for x seconds to character controller
    /// </summary>
    /// <param name="inputs"></param>
    /// <param name="rotation"></param>
    /// <param name="delta"></param>
    private void Move(bool[] inputs, Quaternion rotation, float delta)
    {
        if (delta <= 0.0f) return;

        Vector2 _inputDirection = Vector2.zero;
        if (inputs[0])
        {
            _inputDirection.y += 1;
        }
        if (inputs[1])
        {
            _inputDirection.y -= 1;
        }
        if (inputs[2])
        {
            _inputDirection.x -= 1;
        }
        if (inputs[3])
        {
            _inputDirection.x += 1;
        }

        if (_inputDirection.magnitude > 1.0f) _inputDirection /= _inputDirection.magnitude;

        transform.rotation = rotation;
        Vector3 moveDirection = (transform.right * _inputDirection.x * moveSpeed) + (transform.forward * _inputDirection.y * moveSpeed);

        if (characterController.isGrounded)
        {
            yVelocity = 0;
            if (inputs[4]) { yVelocity = jumpSpeed; }
        }

        moveDirection.y = yVelocity;

        yVelocity -= gravity * delta;

        characterController.Move(moveDirection * delta);
        animationController.Move(new Vector3(_inputDirection.x, 0.0f, _inputDirection.y), transform.forward);
    }

    /// <summary>
    /// Add input & rotation at _time to unprocessedInput list
    /// </summary>
    /// <param name="_inputs"></param>
    /// <param name="_rotation"></param>
    /// <param name="_time"></param>
    public void AddInput(bool[] inputs, Quaternion rotation, float time)
    {
        unprocessedUserInput.Add(new UserInput { Inputs = inputs, Time = time, Rotation = rotation });
    }

    /// <summary>
    /// Set position back in time by delay x
    /// </summary>
    /// <param name="delay"></param>
    public void DelayPosition(float delay)
    {
        float time = syncedTime.GetClientTime() - delay;
        IEnumerable<PlayerState> t = ticks.Where(tick => tick.Time < time);
        if (t.Count() == 0) return;
        PlayerState previous = t.OrderBy(ti => ti.Time).Last();
        int prevIndex = ticks.IndexOf(previous);
        Vector3 nextPos = transform.position;
        float nextTime = Time.time;
        if (prevIndex < ticks.Count - 1)
        {
            PlayerState next = ticks[prevIndex + 1];
            nextPos = next.Position;
            nextTime = next.Time;
        }
        transform.position = Vector3.Lerp(previous.Position, nextPos, (time - previous.Time) / (nextTime - previous.Time));
    }

    /// <summary>
    /// Rewind animation with x delay
    /// </summary>
    /// <param name="delay"></param>
    public void RewindAnimation(float delay)
    {
        float time = syncedTime.GetClientTime() - delay;
        IEnumerable<PlayerState> t = ticks.Where(tick => tick.Time < time);
        if (t.Count() == 0) return;
        PlayerState previous = t.OrderBy(ti => ti.Time).Last();
        int prevIndex = ticks.IndexOf(previous);
        float nextTime = Time.time;
        float nextAnimationTime = animationController.GetNormalizedTime();
        Vector3 nextPosition = transform.position;
        Quaternion nextRotation = transform.rotation;
        if (prevIndex < ticks.Count - 1)
        {
            PlayerState next = ticks[prevIndex + 1];
            nextTime = next.Time;
            nextAnimationTime = next.AnimationTime;
            nextPosition = next.Position;
            nextRotation = next.Rotation;
        }
        
        float lerp = (time - previous.Time) / (nextTime - previous.Time);
        float prevAnimationTime = previous.AnimationTime;
        if (prevAnimationTime > nextAnimationTime) prevAnimationTime = 0.0f;
        float normalizedTime = Mathf.Lerp(prevAnimationTime, nextAnimationTime, lerp);
        animationController.RewindAnimation(normalizedTime, nextPosition - previous.Position, nextRotation * Vector3.forward);
    }

    /// <summary>
    /// Restore character to latest tick
    /// </summary>
    public void SetLatestTick()
    {
        transform.position = this.ticks.Last().Position;
        transform.rotation = this.ticks.Last().Rotation;
        yVelocity = this.ticks.Last().YVelocity;
        animationController.RestoreAnimation(this.ticks.Last().Position - this.ticks[this.ticks.Count - 2].Position, this.transform.forward);
    }

    /// <summary>
    /// Change Holding Gun
    /// </summary>
    /// <param name="gunId"></param>
    /// <param name="time"></param>
    public void ChangeGun(int gunId, float time)
    {
        gun.SetGun(gunId, time, syncedTime.GetClientTime());
    }

    /// <summary>
    /// Shoot a bullet
    /// </summary>
    /// <param name="viewDirection"></param>
    /// <param name="time"></param>
    /// <param name="enemyInterpolationDelay"></param>
    public void Shoot(Vector3 viewDirection, float time, float enemyInterpolationDelay)
    {
        if (health <= 0f) return;
        gun.Shoot(viewDirection, this, time, enemyInterpolationDelay);
    }

    /// <summary>
    /// Throw an item
    /// </summary>
    /// <param name="viewDirection"></param>
    public void ThrowItem(Vector3 viewDirection)
    {
        if (health <= 0f) return;
        if (itemAmount > 0)
        {
            itemAmount--;
            NetworkManager.instance.InstantiateProjectile(shootOrigin).Initialize(viewDirection, throwForce, id);
        }
    }

    /// <summary>
    /// Take hit
    /// </summary>
    /// <param name="damage"></param>
    public bool TakeDamage(float damage, out float damageDealt)
    {
        damageDealt = 0.0f;
        if (health <= 0f) return true;

        damageDealt = damage;
        health -= damage;
        bool dead = false;
        if (health <= 0f) //Died
        {
            dead = true;
            damageDealt += health;
            score.deaths++;
            Die();
            StartCoroutine(Respawn(respawnTime));
        }

        ServerSend.PlayerHealth(this);
        return dead;
    }

    public bool IsDead()
    {
        return health <= 0f;
    }

    /// <summary>
    /// Disable colliders & characterController
    /// Set new spawn position
    /// </summary>
    public void Die()
    {
        health = 0f;
        characterController.enabled = false;
        yVelocity = 0;
        colliders.SetActive(false);
        SetSpawnPosition();
    }

    /// <summary>
    /// Respawn character after x seconds
    /// </summary>
    /// <returns></returns>
    private IEnumerator Respawn(float _respawnTime)
    {
        yield return new WaitForSeconds(_respawnTime);

        health = maxHealth;
        characterController.enabled = true;
        colliders.SetActive(true);
        ServerSend.PlayerRespawned(this);
    }

    /// <summary>
    /// Try to pickup an item
    /// </summary>
    /// <returns></returns>
    public bool AttemptPickupItem()
    {
        if (itemAmount >= maxItemAmount) return false;
        itemAmount++;
        return true;
    }
}
