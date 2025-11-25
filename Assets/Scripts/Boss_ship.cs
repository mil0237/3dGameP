using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random; // UnityEngine.Random으로 고정

public class Boss_ship : MonoBehaviour
{
    public enum BossState { Idle, Pattern1, Pattern2, Pattern3, Pattern4 }

    [Header("Refs")]
    public Transform player;
    public Transform fireLeft;
    public Transform fireRight;
    public Transform fireCenter;
    public GameObject bulletPrefab;
    public GameObject bombPrefab;
    public GameObject warningPrefab;

    [Header("Common")]
    public float zDirectionSign = -1f;
    public float patternGap = 0.6f;
    public bool loopPatterns = true;

    [Header("Pattern1 - Continuous Side Fire")]
    public float p1_fireRate = 0.12f;
    public float p1_bulletSpeed = 20f;
    public float p1_bulletLife = 3.5f;

    [Header("Pattern2 - Fan Spread")]
    public float p2_preDelay = 2.0f;
    public int p2_waves = 4;
    public float p2_waveInterval = 0.35f;
    public int p2_bulletsPerWave = 11;
    public float p2_arcDegrees = 70f;
    public float p2_bulletSpeed = 24f;
    public float p2_bulletLife = 3.2f;

    [Header("Pattern3 - Bombardment with Telegraph")]
    public int p3_count = 6;
    public float p3_warnTime = 0.9f;
    public float p3_spawnHeight = 15f;
    public float p3_forwardOffset = 18f;
    public float p3_fallSpeed = 28f;
    public float p3_bombLife = 4f;
    public float p3_laneWidth = 6f;
    public float p3_randomJitter = 0.6f;

    [Header("Pattern4 - Charge (Z -5) and Return")]
    public float p4_anticipation = 0.5f;
    public float p4_chargeDuration = 0.45f;
    public float p4_returnDuration = 0.6f;
    public AnimationCurve p4_chargeEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve p4_returnEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    BossState _state = BossState.Idle;
    Coroutine _mainLoop;
    Coroutine _p1Loop;
    Vector3 _spawnPos;
    bool _isRunning;

    void Awake() { _spawnPos = transform.position; }
    void OnEnable() => StartBoss();
    void OnDisable() => StopBoss();

    public void StartBoss()
    {
        StopBoss();
        _isRunning = true;
        _mainLoop = StartCoroutine(MainLoop());
    }

    public void StopBoss()
    {
        _isRunning = false;
        if (_p1Loop != null) { StopCoroutine(_p1Loop); _p1Loop = null; }
        if (_mainLoop != null) { StopCoroutine(_mainLoop); _mainLoop = null; }
        _state = BossState.Idle;
    }

    IEnumerator MainLoop()
    {
        yield return new WaitForSeconds(0.5f);

        do
        {
            _state = BossState.Pattern1;
            _p1Loop = StartCoroutine(Pattern1_SideContinuous());
            yield return new WaitForSeconds(3.0f);

            if (_p1Loop != null) { StopCoroutine(_p1Loop); _p1Loop = null; }
            _state = BossState.Pattern2;
            yield return StartCoroutine(Pattern2_FanSpread());
            yield return new WaitForSeconds(patternGap);

            _state = BossState.Pattern3;
            yield return StartCoroutine(Pattern3_Bombardment());
            yield return new WaitForSeconds(patternGap);

            _state = BossState.Pattern4;
            yield return StartCoroutine(Pattern4_ChargeAndReturn());
            yield return new WaitForSeconds(patternGap);

        } while (_isRunning && loopPatterns);

        if (_p1Loop != null) { StopCoroutine(_p1Loop); _p1Loop = null; }
        _state = BossState.Idle;
    }

    IEnumerator Pattern1_SideContinuous()
    {
        while (true)
        {
            FireBullet(fireLeft.position, Vector3.forward * zDirectionSign, p1_bulletSpeed, p1_bulletLife);
            FireBullet(fireRight.position, Vector3.forward * zDirectionSign, p1_bulletSpeed, p1_bulletLife);
            yield return new WaitForSeconds(p1_fireRate);
        }
    }

    IEnumerator Pattern2_FanSpread()
    {
        yield return new WaitForSeconds(p2_preDelay);

        for (int w = 0; w < p2_waves; w++)
        {
            float start = -p2_arcDegrees * 0.5f;
            float step = (p2_bulletsPerWave <= 1) ? 0f : (p2_arcDegrees / (p2_bulletsPerWave - 1));
            for (int i = 0; i < p2_bulletsPerWave; i++)
            {
                float ang = start + step * i;
                Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward * zDirectionSign;
                FireBullet(fireCenter.position, dir, p2_bulletSpeed, p2_bulletLife);
            }
            yield return new WaitForSeconds(p2_waveInterval);
        }
    }

    IEnumerator Pattern3_Bombardment()
    {
        if (!player) yield break;

        float[] xs = new float[p3_count];
        for (int i = 0; i < p3_count; i++)
        {
            xs[i] = Random.Range(-p3_laneWidth, p3_laneWidth);
            xs[i] += Random.Range(-p3_randomJitter, p3_randomJitter);
        }

        GameObject[] warns = new GameObject[p3_count];
        for (int i = 0; i < p3_count; i++)
        {
            if (!warningPrefab) continue;
            var w = Instantiate(warningPrefab);
            w.transform.position = new Vector3(xs[i], player.position.y, player.position.z);
            warns[i] = w;
        }

        yield return new WaitForSeconds(p3_warnTime);

        for (int i = 0; i < p3_count; i++)
        {
            if (warns[i]) Destroy(warns[i]);

            Vector3 spawn = new Vector3(xs[i], player.position.y + p3_spawnHeight, player.position.z + p3_forwardOffset);
            Vector3 dir = (Vector3.down + Vector3.back * 0.35f).normalized * Mathf.Sign(zDirectionSign);
            FireBomb(spawn, dir, p3_fallSpeed, p3_bombLife);
        }
    }

    //  여기 수정됨: Z -5 이동
    IEnumerator Pattern4_ChargeAndReturn()
    {
        yield return new WaitForSeconds(p4_anticipation);

        Vector3 start = transform.position;
        Vector3 target = new Vector3(start.x, start.y, start.z - 5f); // Z축 -5 이동

        float t = 0f;
        while (t < p4_chargeDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / p4_chargeDuration);
            transform.position = Vector3.Lerp(start, target, p4_chargeEase.Evaluate(u));
            yield return null;
        }

        t = 0f;
        Vector3 leave = transform.position;
        while (t < p4_returnDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / p4_returnDuration);
            transform.position = Vector3.Lerp(leave, _spawnPos, p4_returnEase.Evaluate(u));
            yield return null;
        }

        transform.position = _spawnPos;
    }

    // === SimpleProjectile 전용 발사 ===
    void FireBullet(Vector3 pos, Vector3 dir, float speed, float life)
    {
        if (!bulletPrefab) return;
        var go = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
        var sp = go.GetComponent<SimpleProjectile>() ?? go.AddComponent<SimpleProjectile>();
        sp.Init(dir.normalized, speed, life);
    }

    void FireBomb(Vector3 pos, Vector3 dir, float speed, float life)
    {
        if (!bombPrefab) return;
        var go = Instantiate(bombPrefab, pos, Quaternion.LookRotation(dir));
        var sp = go.GetComponent<SimpleProjectile>() ?? go.AddComponent<SimpleProjectile>();
        sp.Init(dir.normalized, speed, life);
    }
}
