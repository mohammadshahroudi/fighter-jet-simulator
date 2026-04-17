using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * This script is responsible for reading a level layout from a text file and constructing the level
 * in a Unity scene by instantiating block GameObjects. The level file should be placed in the
 * Resources folder, and each line in the file represents a row of blocks.
 *
 * WHAT YOU NEED TO DO:
 * 1. In the for loop that iterates over each character (i.e. letter) in the current row, determine
 *    which type of block to create based on the letter (e.g., use 'R' for rock, 'B' for brick, etc.).
 *
 * 2. Instantiate the correct prefab (rockPrefab, brickPrefab, questionBoxPrefab, stonePrefab) corresponding
 *    to the letter.
 *
 * 3. Calculate the position for the new block GameObject using the current row and column index.
 *    - You will likely need to maintain a separate column counter as you iterate through the characters.
 *
 * 4. Set the instantiated block’s parent to 'environmentRoot' to keep the hierarchy organized.
 *
 * ADDITIONAL NOTES:
 * - The level reloads when the player presses the 'R' key, which clears all blocks under levelRoot
 *   and then re-parses the level file.
 * - Ensure that the level file's name (without the extension) matches the 'filename' variable.
 *
 * By completing these TODOs, you will enable the level parser to dynamically create and position
 * the blocks based on the level file data.
 */


public class LevelParser : MonoBehaviour
{
    public TextAsset levelFile;
    public Transform levelRoot;

    [Header("Prefabs")]
    public GameObject airportChunkPrefab;
    public GameObject runwayChunkPrefab;
    public GameObject baseChunkPrefab;
    // public GameObject waterChunkPrefab;

    [Header("Chunk Generation")] 
    public float x_gap = 1000f;
    public float z_gap = 1000f;

    // private AudioController audioController;

    
    void Start()
    {
        LoadLevel();
        
        /*
        if (audioController != null)
        {
            audioController.PlayBackgroundMusicLoop(); // Play the background music
        }
        */
    }

    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
            ReloadLevel();
    }

    void LoadLevel()
    {
        // Push lines onto a stack so we can pop bottom-up rows. This is easy to reason
        //  about, but an index-based loop over the string array is faster.
        Stack<string> levelRows = new Stack<string>();

        foreach (string line in levelFile.text.Split('\n'))
            levelRows.Push(line);

        int airportRow = 0;
        int airportColumn = 0;
        bool isThereAnAirport = false;
        
        string[] lines = levelFile.text.Split('\n');
        int rowToSearch = 0;

        
        // This handles trying to find the airport prefab which will always be in the middle of the txt file given
        foreach (string line in lines)
        {
            string newLine = line.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            
            char[] chars = newLine.ToCharArray();
            int gridColumn = 0;

            for (int i = 0; i < chars.Length; i++)
            {
                char currentChar = chars[i];

                if (currentChar == ' ')
                {
                    continue;
                }

                
                // Save the row and golumn to be used later
                if (currentChar == 'A')
                {
                    airportRow = rowToSearch;
                    airportColumn = gridColumn;
                    isThereAnAirport = true;
                    break;
                }
                
                gridColumn++;
            }

            if (isThereAnAirport)
            {
                break;
            }

            rowToSearch++;
        }

        int row = 0;
        while (levelRows.Count > 0)
        {
            string rowString = levelRows.Pop();
            char[] rowChars = rowString.ToCharArray();

            int gridColumn = 0;
            
            for (var columnIndex = 0; columnIndex < rowChars.Length; columnIndex++)
            {
                var currentChar = rowChars[columnIndex];

                // Todo - Instantiate a new GameObject that matches the type specified by the character
                // Todo - Position the new GameObject at the appropriate location by using row and column
                // Todo - Parent the new GameObject under levelRoot
                
                // Ignore space
                if (currentChar == ' ')
                {
                    continue;
                }
                
                // Create x and z references to be used for generation
                float x = (gridColumn - airportColumn) * x_gap;
                float z = (row - airportRow) * z_gap;
                Vector3 newPosition = new Vector3(x, 0, z); // Moved from the if statemnts to here
                
                if (currentChar == 'A')
                {
                    Transform airportChunkInstance = Instantiate(airportChunkPrefab, levelRoot).transform;
                    airportChunkInstance.position = newPosition;
                    airportChunkInstance.tag = "AirportChunk"; // Tag the chunk with its appropriate tag
                }
                
                if (currentChar == 'R')
                {
                    Transform runwayChunkInstance = Instantiate(runwayChunkPrefab, levelRoot).transform;
                    runwayChunkInstance.position = newPosition;
                    runwayChunkInstance.tag = "RunwayChunk"; // Tag the chunk with its appropriate tag
                }
                
                if (currentChar == 'B')
                {
                    Transform baseChunkInstance = Instantiate(baseChunkPrefab, levelRoot).transform;
                    baseChunkInstance.position = newPosition;
                    baseChunkInstance.tag = "BaseChunk"; // Tag the chunk with its appropriate tag
                }
                
                // This is the only object that should ideally "destroy" the player
                // Left unused for later
                /*
                if (currentChar == 'W')
                {
                    Vector3 newPosition = new Vector3(columnIndex + 0.5f, row + 0.5f, 0);
                    Transform waterChunkInstance = Instantiate(waterChunkPrefab, levelRoot).transform;
                    waterChunkInstance.position = newPosition;
                    waterChunkInstance.tag = "WaterChunk"; // Tag the bricks with a Water tag
                }
                */

                gridColumn++;

            }  

            row++;
        }
    }

    // --------------------------------------------------------------------------
    void ReloadLevel()
    {
        foreach (Transform child in levelRoot)
           Destroy(child.gameObject);
        
        LoadLevel();
    }
}



