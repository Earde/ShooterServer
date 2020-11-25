using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int id;
    public string username;
    public CharacterController characterController;
    public Transform shootOrigin;
    public float gravity = -9.81f;
    public float moveSpeed = 5f;
    public float jumpSpeed = 5f;
    public float throwForce = 600f;
    public float health;
    public float maxHealth = 100.0f;
    public int itemAmount = 0;
    public int maxItemAmount = 3;

    public float respawnTime = 5f;
    public Vector3 spawnPosition = new Vector3(20.5f, 2.5f, -1.8f);

    public GameObject colliders;

    public Gun gun;

    public AnimationController animationController;

    private float saveTime = 1f; //time in seconds after inputs and ticks will be discarded
    private ThreadSafeList<UserInput> unprocessedUserInput = new ThreadSafeList<UserInput>(new List<UserInput>());
    private List<UserInput> processedUserInput = new List<UserInput>();
    private List<PlayerState> ticks = new List<PlayerState>();
    private float yVelocity;

    public SyncedTime syncedTime;

    private bool DEBUG = false;

    private void Start()
    {
        Debug.Log(Time.fixedDeltaTime);
    }

    public void UpdateSyncTime(int packetId, float clientTime)
    {
        syncedTime.UpdateTime(packetId, clientTime);
    }

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
        health = maxHealth;
        yVelocity = 0f;
        transform.position = spawnPosition;
    }

    private void InitTicks()
    {
        for (float x = saveTime; x > 0.0f; x -= Time.fixedDeltaTime)
        {
            processedUserInput.Add(new UserInput { inputs = new bool[5], time = syncedTime.GetClientTime() - x, rotation = Quaternion.identity });
            unprocessedUserInput.Add(new UserInput { inputs = new bool[5], time = syncedTime.GetClientTime() - x, rotation = Quaternion.identity });
            ticks.Add(new PlayerState { _position = spawnPosition, _rotation = Quaternion.identity, _time = syncedTime.GetClientTime() - x, _yVelocity = yVelocity });
        }
    }

    public void FixedUpdate()
    {
        //Is time synced?
        syncedTime.SendTimePacket(id);
        if (!syncedTime.isReady) return;
        //Initialize ticks
        if (ticks.Count == 0) InitTicks();
        //Get client time
        float currentTime = syncedTime.GetClientTime();
        //Clone multithreaded lists
        List<UserInput> tempUnprocessedUserInput = unprocessedUserInput.Clone();
        tempUnprocessedUserInput.RemoveAll(u => u.time < ticks.First()._time || u.time < currentTime - saveTime);
        unprocessedUserInput = new ThreadSafeList<UserInput>(tempUnprocessedUserInput);
        List<UserInput> uInput = unprocessedUserInput.Clone();
        //Is Alive
        if (health > 0f)
        {
            //Has new client input
            if (uInput.Count > 0)
            {
                Vector2 _inputDirection = Vector2.zero;
                uInput = uInput.OrderBy(x => x.time).ToList();
                //Merge processed + unprocessed inputs
                List<UserInput> allInputs = new List<UserInput>();
                allInputs.AddRange(processedUserInput);
                allInputs.AddRange(uInput);

                //Reconcilate
                if (uInput.First().time < ticks.Last()._time) 
                {
                    //Rewind ticks till oldest unprocessedUserInput
                    ticks.RemoveAll(t => t._time > uInput.First().time);
                    //Set last valid tick information
                    characterController.enabled = false;
                    characterController.transform.position = ticks.Last()._position;
                    characterController.transform.rotation = ticks.Last()._rotation;
                    yVelocity = ticks.Last()._yVelocity;
                    characterController.enabled = true;
                    characterController.Move(Vector3.zero); //Reset isGrounded

                    float newTickTime;
                    do
                    {
                        float lastTickTime = ticks.Last()._time;
                        newTickTime = lastTickTime + Time.fixedDeltaTime;
                        if (newTickTime > currentTime) { newTickTime = currentTime; }
                        List<UserInput> movesThisTick = allInputs.Where(a => a.time > lastTickTime && a.time <= newTickTime).OrderBy(ui => ui.time).ToList();
                        UserInput prevTickMove = allInputs.Where(a => a.time <= lastTickTime).OrderByDescending(x => x.time).FirstOrDefault();
                        if (prevTickMove != default && prevTickMove != null)
                        {
                            prevTickMove.time = ticks.Last()._time;
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
                                    moveTime = newTickTime - movesThisTick[i].time;
                                }
                                else
                                {
                                    moveTime = movesThisTick[i + 1].time - movesThisTick[i].time;
                                }
                                Move(movesThisTick[i].inputs, movesThisTick[i].rotation, moveTime);
                            }
                        }
                        ticks.Add(new PlayerState { _position = transform.position, _rotation = transform.rotation, _time = newTickTime, _yVelocity = yVelocity });
                    } while (newTickTime != currentTime);
                }
                else
                {
                    //Execute last ProcessedInput from last tick till first UnProcessedInput
                    float moveTime = uInput.First().time - ticks.Last()._time;
                    Move(processedUserInput.Last().inputs, processedUserInput.Last().rotation, moveTime);
                    //Execute unprocessed Input
                    for (int i = 0; i < uInput.Count; i++)
                    {
                        if (i == uInput.Count - 1)
                        {
                            moveTime = currentTime - uInput[i].time;
                        }
                        else
                        {
                            moveTime = uInput[i + 1].time - uInput[i].time;
                        }
                        Move(uInput[i].inputs, uInput[i].rotation, moveTime);
                    }
                    //Add server tick
                    ticks.Add(new PlayerState { _position = transform.position, _rotation = transform.rotation, _time = currentTime, _yVelocity = yVelocity });
                }
            }
            //NO new unprocessed input
            //Execute last processed input & create server tick
            else
            {
                Move(processedUserInput.Last().inputs, processedUserInput.Last().rotation, currentTime - ticks.Last()._time);
                ticks.Add(new PlayerState { _position = transform.position, _rotation = transform.rotation, _time = currentTime, _yVelocity = yVelocity });
            }
        }
        //Is Dead
        else
        {
            yVelocity = 0;
            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;
            ticks.Add(new PlayerState { _position = transform.position, _rotation = transform.rotation, _time = currentTime, _yVelocity = yVelocity });
        }

        //move from unprocessed to processed
        foreach (UserInput u in uInput)
        {
            unprocessedUserInput.Remove(u);
            processedUserInput.Add(u);
        }
        if (ticks.Count > saveTime / Time.fixedDeltaTime) ticks.RemoveAt(0);
        while (processedUserInput.Count > saveTime / Time.fixedDeltaTime) processedUserInput.RemoveAt(0);
        processedUserInput = processedUserInput.OrderBy(pui => pui.time).ToList();
        //send new tick to all players
        ServerSend.PlayerPosition(id, ticks.Last());

        if (DEBUG) Logs();
    }

    /// <summary>
    /// Debug logs
    /// </summary>
    private void Logs()
    {
        Debug.Log("pInput Count: " + processedUserInput.Count);
        Debug.Log("uInput Count: " + unprocessedUserInput.Count);
        Debug.Log("ticks Count: " + ticks.Count);
    }

    /// <summary>
    /// Apply keyboard input and mouse rotation for x seconds to character controller
    /// </summary>
    /// <param name="inputs"></param>
    /// <param name="rotation"></param>
    /// <param name="moveDuration"></param>
    private void Move(bool[] inputs, Quaternion rotation, float moveDuration)
    {
        if (moveDuration <= 0.0f) return;

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
        if (_inputDirection.magnitude > 1.5f)
        {
            _inputDirection *= 0.7071f; //Keer wortel van 0.5 om _inputDirection.magnitude van 1 te krijgen
        }

        transform.rotation = rotation;
        Vector3 moveDirection = (transform.right * _inputDirection.x * moveSpeed) + (transform.forward * _inputDirection.y * moveSpeed);

        if (characterController.isGrounded)
        {
            yVelocity = 0;
            if (inputs[4]) { yVelocity = jumpSpeed; }
        }

        moveDirection.y = yVelocity;

        if (!characterController.isGrounded)
        {
            yVelocity -= gravity * moveDuration;
        }

        characterController.Move(moveDirection * moveDuration);
    }

    public void AddInput(bool[] _inputs, Quaternion _rotation, float _time)
    {
        unprocessedUserInput.Add(new UserInput { inputs = _inputs, time = _time, rotation = _rotation });
    }

    /// <summary>
    /// Set position back in time by delay x
    /// </summary>
    /// <param name="delay"></param>
    public void DelayPosition(float delay)
    {
        float time = syncedTime.GetClientTime() - delay;
        IEnumerable<PlayerState> t = ticks.Where(tick => tick._time < time);
        if (t.Count() == 0) return;
        PlayerState previous = t.OrderBy(ti => ti._time).Last();
        int prevIndex = ticks.IndexOf(previous);
        if (prevIndex == ticks.Count - 1) return;
        PlayerState next = ticks[prevIndex + 1];
        transform.position = Vector3.Lerp(previous._position, next._position, (time - previous._time) / (next._time - previous._time));
    }

    public void RewindAnimation(float delay)
    {
        float time = syncedTime.GetClientTime() - delay;
        IEnumerable<PlayerState> t = ticks.Where(tick => tick._time < time);
        if (t.Count() == 0) return;
        PlayerState previous = t.OrderBy(ti => ti._time).Last();
        int prevIndex = ticks.IndexOf(previous);
        if (prevIndex == ticks.Count - 1) return;
        PlayerState next = ticks[prevIndex + 1];
        float lerp = (time - previous._time) / (next._time - previous._time);
        animationController.RewindAnimation(lerp, next._position - previous._position, next._rotation * Vector3.forward);
    }

    /// <summary>
    /// Restore character to latest tick
    /// </summary>
    public void SetLatestTick()
    {
        transform.position = this.ticks.Last()._position;
        transform.rotation = this.ticks.Last()._rotation;
        yVelocity = this.ticks.Last()._yVelocity;
        animationController.RestoreAnimation(this.ticks.Last()._position - this.ticks[this.ticks.Count - 2]._position, this.transform.forward);
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
        gun.Shoot(viewDirection, shootOrigin, this, time, enemyInterpolationDelay);
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
    public void TakeDamage(float damage)
    {
        if (health <= 0f) return;

        health -= damage;
        if (health <= 0f) //Died
        {
            Die();
            StartCoroutine(Respawn(respawnTime));
        }

        ServerSend.PlayerHealth(this);
    }

    public void Die()
    {
        health = 0f;
        characterController.enabled = false;
        yVelocity = 0;
        colliders.SetActive(false);
        transform.position = spawnPosition;
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
