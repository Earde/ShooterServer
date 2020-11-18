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

    public Gun gun;

    private float saveTime = 3f; //time in seconds after inputs and ticks will be discarded
    private ThreadSafeList<UserInput> unprocessedUserInput = new ThreadSafeList<UserInput>(new List<UserInput>());
    private List<UserInput> processedUserInput = new List<UserInput>();
    private List<PlayerState> ticks = new List<PlayerState>();
    private float yVelocity;

    private SyncedTime syncedTime = new SyncedTime();

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
        StartCoroutine(SendTimeSync());
        transform.position = spawnPosition;
    }

    private IEnumerator SendTimeSync()
    {
        yield return new WaitForSeconds(1.0f);

        syncedTime.SendTimePacket(id);
    }

    private void InitTicks()
    {
        for (float x = saveTime; x > 0.0f; x -= Time.fixedDeltaTime)
        {
            unprocessedUserInput.Add(new UserInput { inputs = new bool[5], time = syncedTime.GetTime() - x, rotation = Quaternion.identity });
            ticks.Add(new PlayerState { position = spawnPosition, rotation = Quaternion.identity, time = syncedTime.GetTime() - x, _yVelocity = yVelocity });
        }
    }

    public void FixedUpdate()
    {
        if (!syncedTime.isReady) return;
        if (ticks.Count == 0) InitTicks();
        float currentTime = syncedTime.GetTime();
        List<UserInput> tempUnprocessedUserInput = unprocessedUserInput.Clone();
        tempUnprocessedUserInput.RemoveAll(u => u.time < ticks.First().time || u.time < currentTime - saveTime);
        unprocessedUserInput = new ThreadSafeList<UserInput>(tempUnprocessedUserInput);
        List<UserInput> uui = unprocessedUserInput.Clone();
        if (health > 0f)
        {
            if (uui.Count > 0)
            {
                Vector2 _inputDirection = Vector2.zero;
                uui = uui.OrderBy(x => x.time).ToList();
                if (uui[0].time < ticks.Last().time) //reconcilate
                {
                    //Debug.Log("Reconcilating for " + id);
                    //rewind ticks till oldest unprocessedUserInput
                    ticks.RemoveAll(t => t.time > uui[0].time);
                    characterController.enabled = false;
                    characterController.transform.position = ticks.Last().position;
                    characterController.enabled = true;
                    List<UserInput> allInputs = processedUserInput.Concat(uui).ToList();
                    float newTickTime;
                    do
                    {
                        float lastTickTime = ticks.Last().time;
                        newTickTime = lastTickTime + Time.fixedDeltaTime;
                        if (newTickTime > currentTime) { newTickTime = currentTime; }
                        List<UserInput> movesThisTick = allInputs.Where(a => a.time > lastTickTime && a.time <= newTickTime).OrderBy(ui => ui.time).ToList();
                        List<UserInput> previousTickMoves = allInputs.Where(a => a.time < lastTickTime).OrderBy(x => x.time).ToList();
                        if (movesThisTick.Count > 0)
                        {
                            //execute last ProcessedInput input from last tick time till first UnProcessedInput time
                            float moveTime = movesThisTick[0].time - lastTickTime;
                            if (previousTickMoves.Count > 0)
                            {
                                Move(previousTickMoves.Last().inputs, previousTickMoves.Last().rotation, moveTime);
                            }
                            for (int i = 0; i < movesThisTick.Count; i++)
                            {
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
                        ticks.Add(new PlayerState { position = transform.position, rotation = transform.rotation, time = newTickTime, _yVelocity = yVelocity });
                    } while (newTickTime != currentTime);
                }
                else
                {
                    //Debug.Log("Processing last tick for " + id);
                    //execute last ProcessedInput input from last tick time till first UnProcessedInput time
                    float moveTime = uui[0].time - ticks.Last().time;
                    Move(processedUserInput.Last().inputs, processedUserInput.Last().rotation, moveTime);
                    for (int i = 0; i < uui.Count; i++)
                    {
                        if (i == uui.Count - 1)
                        {
                            moveTime = currentTime - uui[i].time;
                        }
                        else
                        {
                            moveTime = uui[i + 1].time - uui[i].time;
                        }
                        Move(uui[i].inputs, uui[i].rotation, moveTime);
                    }
                    ticks.Add(new PlayerState { position = transform.position, rotation = transform.rotation, time = currentTime, _yVelocity = yVelocity });
                }
            } else
            {
                Move(processedUserInput.Last().inputs, processedUserInput.Last().rotation, currentTime - ticks.Last().time);
                ticks.Add(new PlayerState { position = transform.position, rotation = transform.rotation, time = currentTime, _yVelocity = yVelocity });
            }
        }
        else
        {
            yVelocity = 0;
            transform.position = spawnPosition;
            ticks.Add(new PlayerState { position = transform.position, rotation = transform.rotation, time = currentTime, _yVelocity = yVelocity });
        }

        //move from unprocessed to processed
        foreach (UserInput u in uui)
        {
            unprocessedUserInput.Remove(u);
            processedUserInput.Add(u);
        }
        if (ticks.Count > saveTime / Time.fixedDeltaTime) ticks.RemoveAt(0);
        if (processedUserInput.Count > saveTime / Time.fixedDeltaTime) processedUserInput.RemoveAt(0);
        processedUserInput = processedUserInput.OrderBy(pui => pui.time).ToList();
        //Debug.Log($"{ticks.Count} ticks, {processedUserInput.Count} pInput");
        //send new tick to all players
        ServerSend.PlayerPosition(id, ticks.Last());
    }

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

    public void SetPositionInTime(float time)
    {
        IEnumerable<PlayerState> t = ticks.Where(tick => tick.time < time);
        if (t.Count() == 0) return;
        PlayerState previous = t.Last();
        int prevIndex = ticks.IndexOf(previous);
        if (prevIndex == ticks.Count - 1) return;
        PlayerState next = ticks[prevIndex + 1];
        transform.position = Vector3.Lerp(previous.position, next.position, (time - previous.time) / (next.time - previous.time));
    }

    public void SetLatestPosition()
    {
        transform.position = this.ticks.Last().position;
    }

    public void ChangeGun(int gunId, float time)
    {
        gun.SetGun(gunId, time, syncedTime.GetTime());
    }

    public void Shoot(Vector3 viewDirection, float time, float enemyTime)
    {
        if (health <= 0f) return;
        gun.Shoot(viewDirection, shootOrigin, time, enemyTime, id);
    }

    public void ThrowItem(Vector3 viewDirection)
    {
        if (health <= 0f) return;
        if (itemAmount > 0)
        {
            itemAmount--;
            NetworkManager.instance.InstantiateProjectile(shootOrigin).Initialize(viewDirection, throwForce, id);
        }
    }

    public void TakeDamage(float damage)
    {
        if (health <= 0f)
        {
            return;
        }

        health -= damage;
        if (health <= 0f) //Died
        {
            health = 0f;
            characterController.enabled = false;
            yVelocity = 0;
            transform.position = spawnPosition; 
            StartCoroutine(Respawn());
        }

        ServerSend.PlayerHealth(this);
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);

        health = maxHealth;
        characterController.enabled = true;
        ServerSend.PlayerRespawned(this);
    }

    public bool AttemptPickupItem()
    {
        if (itemAmount >= maxItemAmount) return false;
        itemAmount++;
        return true;
    }
}
