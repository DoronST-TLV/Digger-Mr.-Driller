using UnityEngine;

namespace Strata
{
    /// <summary>Random camera offset that decays to zero. Sits on the camera (child of the rig) and only touches localPosition.</summary>
    public class ScreenShake : MonoBehaviour
    {
        private float strength;
        private float duration;
        private float timeLeft;

        public void Shake(float newStrength, float newDuration)
        {
            strength = Mathf.Max(strength, newStrength);
            duration = Mathf.Max(newDuration, 0.01f);
            timeLeft = newDuration;
        }

        private void LateUpdate()
        {
            float z = transform.localPosition.z;
            if (timeLeft <= 0f)
            {
                strength = 0f;
                transform.localPosition = new Vector3(0f, 0f, z);
                return;
            }
            timeLeft -= Time.deltaTime;
            Vector2 offset = Random.insideUnitCircle * strength * (timeLeft / duration);
            transform.localPosition = new Vector3(offset.x, offset.y, z);
        }
    }
}
