using UnityEngine;

namespace RobotStrategy.Movement
{
    // Local, straight-line 2D movement. Supply waypoints from a pathfinder later.
    public class RobotTerrainMovement : MonoBehaviour
    {
        public TerrainMovementMap terrainMap;
        public RobotMovementProfile profile = new RobotMovementProfile();
        [Tooltip("Flying uses sky aptitude over both land and sea.")]
        public bool flying;
        public float CurrentSpeed { get; private set; }
        public bool IsMoving { get; private set; }
        public Vector3 Destination { get; private set; }

        public void ApplyDevelopmentValues(int speed, int ground, int sea, int sky)
            => profile.SetDevelopmentValues(speed, ground, sea, sky);

        public bool SetDestination(Vector3 target)
        {
            target.z = transform.position.z;
            if (terrainMap == null || !terrainMap.TryGetTerrain(target, out _))
                return false;
            Destination = target;
            IsMoving = true;
            return true;
        }

        public void Stop()
        {
            IsMoving = false;
            CurrentSpeed = 0f;
        }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            if (!IsMoving || deltaTime <= 0) return;
            if (terrainMap == null || profile == null) { Stop(); return; }
            // Sample in small spatial steps so fast units do not jump over a sea tile.
            // The boundary approximation is at most 5% of a cell per sample.
            float remaining = deltaTime;
            float sampleDistance = terrainMap.SamplingDistance;
            for (int i = 0; i < 4096 && remaining > 0 && IsMoving; i++)
            {
                Vector3 position = transform.position;
                if (!terrainMap.TryGetTerrain(position, out TerrainKind terrain)) { Stop(); break; }
                float distance = Vector3.Distance(position, Destination);
                if (distance <= 0.00001f) { transform.position = Destination; Stop(); break; }
                CurrentSpeed = profile.GetSpeed(terrain, flying);
                if (CurrentSpeed <= 0) break;
                float step = Mathf.Min(distance, Mathf.Min(sampleDistance, CurrentSpeed * remaining));
                Vector3 next = Vector3.MoveTowards(position, Destination, step);
                if (!terrainMap.TryGetTerrain(next, out _)) { Stop(); break; }
                transform.position = next;
                remaining -= step / CurrentSpeed;
                if (step >= distance) Stop();
            }
        }
    }
}
