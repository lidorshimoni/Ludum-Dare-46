using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    public enum action { Consumable, Ammo }
    [Header("References")]
    public Consumable consumeRef;

    [Header("Options")]
    public action interaction;
    public bool destroy=true;

    [Header("Dissolve")]
    public bool dissolve;
    public float dissolveTime;
    public string DissolveShaderParam = "Vector1_FEFF47F1";

    private bool consumed = false;


    public void DoInteraction(GameObject player)
    {
        if (consumed)
            return;
        consumed = true;
        if (dissolve)
            StartCoroutine(DissolveTransition(0, 1, 1));

        if(destroy)
            Destroy(gameObject, dissolveTime);

        if(interaction == action.Consumable && consumeRef!=null)
        {
            consumeRef.Consume(player);
        }
    }

    private IEnumerator DissolveTransition(float start, float end, float duration)
    {
        var mat = gameObject.GetComponent<Renderer>().material;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime / duration;
            mat.SetFloat(DissolveShaderParam, Mathf.Lerp(start, end, t));
            yield return null;
        }
        mat.SetFloat(DissolveShaderParam, end);
    }
}
