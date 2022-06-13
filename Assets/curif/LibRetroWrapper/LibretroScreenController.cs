/* 
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
*/


#define _debug_

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;


// [AddComponentMenu("curif/LibRetroWrapper/LibretroScreen")]
[RequireComponent(typeof(AudioSource))]
public class LibretroScreenController : MonoBehaviour {
    [SerializeField]
    public string GameFile = "1942.zip";

    [SerializeField]
    public GameObject Player;
    [Tooltip("The minimal distance between the player and the screen to start the game.")]
    [SerializeField]
    public float DistanceMinToPlayerToStartGame = 0.9f;
    [Tooltip("The time in secs that the player has to look to another side to exit the game and recover mobility.")]
    [SerializeField]
    public int SecondsToWaitToExitGame = 3;

    [Tooltip("Adjust Gamma from 1.0 to 2.0")]
    [SerializeField]
    public LibretroMameCore.GammaOptions Gamma = LibretroMameCore.GammaOptions.GAMA_1;
    [Tooltip("Adjust bright from 0.2 to 2.0")]
    [SerializeField]
    public LibretroMameCore.BrightnessOptions Brightness = LibretroMameCore.BrightnessOptions.BRIGHT_1;

    private GameObject Camera;
    private LibretroMameCore.Waiter SecsForCheqClose = new(2);
    // [SerializeField]
    Renderer Display;
    private bool isVisible = false;

    // LibretroMameCore.FpsControl fpsDebug = new(60f);

    // Start is called before the first frame update
    void Start() {
        LibretroMameCore.WriteConsole($"{gameObject.name} Start");

        Camera = GameObject.Find("CenterEyeAnchor");
        if (Camera == null) {
            throw new Exception("Camera not found in GameObject Tree");
        }
        Display = GetComponent<Renderer>();
        Player = GameObject.Find("PlayerController");

    }
    /*
    public void Update() {
        fpsDebug.CountTimeFrame();
        LibretroMameCore.WriteConsole($"{gameObject.name} {fpsDebug.ToString()} visible: {isVisible}c");
        return;
    }
    */

    public void Update() {
        // LibretroMameCore.WriteConsole($"Mame Started? {MameStarted}");
        if (! isVisible) {
            return;
        }
        if (! LibretroMameCore.GameLoaded) {

            if (SecsForCheqClose.Finished()) {
                SecsForCheqClose.reset();
                if (LibretroMameCore.isPlayerClose(Camera, Display, DistanceMinToPlayerToStartGame) && 
                    LibretroMameCore.isPlayerLookingAtScreen(Camera, Display, DistanceMinToPlayerToStartGame)) {

                    //start mame
                    LibretroMameCore.WriteConsole(string.Format("MAME Start game: {0} +_+_+_+_+_+_+_+__+_+_+_+_+_+_+_+_+_+_+_+_", GameFile));
                    LibretroMameCore.DistanceMinToPlayerToStartGame = DistanceMinToPlayerToStartGame;
                    LibretroMameCore.Speaker = GetComponent<AudioSource>();
                    LibretroMameCore.Player = Player;
                    LibretroMameCore.Display = Display;
                    LibretroMameCore.Camera = Camera;
                    LibretroMameCore.SecondsToWaitToExitGame = SecondsToWaitToExitGame;
                    LibretroMameCore.Brightness = Brightness;
                    LibretroMameCore.Gamma = Gamma;
                    LibretroMameCore.Start(GameFile);

                    var inputDevices = new List<UnityEngine.XR.InputDevice>();
                    UnityEngine.XR.InputDevices.GetDevices(inputDevices);
                    foreach (var device in inputDevices) {
                        LibretroMameCore.WriteConsole(string.Format("Device found with name '{0}' ", device.name));
                    }
                }
            }
        }

        //only Runs when my game is loaded
        // LibretroMameCore.WriteConsole($"MAME {GameFile} Libretro {LibretroMameCore.GameFileName} loaded: {LibretroMameCore.GameLoaded}");

        LibretroMameCore.Run(GameFile);

    }

    private void OnAudioFilterRead(float[] data, int channels) {
        LibretroMameCore.MoveAudioStreamTo(GameFile, data);
    }

    private void OnDestroy() {
        LibretroMameCore.End(GameFile);
    }

     void OnBecameVisible()
    {
        isVisible = true;
        SecsForCheqClose.reset();
        //fpsDebug.Reset();
    }
    void OnBecameInvisible()
    {
        isVisible = false;
    }
}