# Nexxtporter
Nexxtporter is a command line tool intended to be used with the excellent [Nexxt by Frankengraphics](https://frankengraphics.itch.io/nexxt). It can be used as part of a build system to automate the process of exporting data such as CHR data, palettes and nametables. 

Nexxt saves it's data (NSS files) in a text based format which is well suited to source control. Integrating Nexxtporter into a build allows one to treat the binary files exported from Nexxt as build artifacts rather than source code.

The currently supported features include:

* Export CHR data to binary files
* Export palette data as assembly source code
* Export nametable data as binary files
* Export nametable pattern data as binary files
* Render pngs using CHR and palette data
* Render pngs of the nametable shown in Nexxt

There is a complete working example of a configuration file at the end of this document.

## Command line usage
A single command line argument must be passed to nexxtporter.exe which contains the JSON configuration file. This can be a relative or fully qualified path.

EG: 

`nexxtporter.exe config.json`

`nexxtporter.exe "c:/code/project2/exportConfig.json"`

## Config file format
Note that throughout the config file when a byte offset, start or size is specified the following formats are accepted:

    "Start": "100"       // Decimal, Start = 100
    "Start": "$100"      // Hex, Start = 256
    "Start": "%10000000" // Binary, Start = 128

The top level structure of the config file is as follows. Full details on each section can be found below along with examples.

    {
        "LogConfig": { }
        "RGBLookupTables": [ ]
        "NSSFiles": [ ]
    }

### LogConfig
Optional

    {
        "Echo: true,
        "LogFile": "log.txt"
    }

#### Attributes
* Echo

  Optional, boolean, defaults to true

  If false, log statements (including errors) are not written to the system console.
* LogFile

  Optional, string, defaults to null

  If set to a filename, the file will overwritten with the log.

### RGBLookupTables
Optional

Note that this data is only used if rendering bitmaps and also if a different set of RGB values are desired from the default (eg: a different region or a palette with ephasis bits set). A set of exactly 64 colors must be provided for each table which will be used to render the NES palette entries.

    [
        {
            "ID": "customRGB",
            "Colors": [
                "#6A6D6A",
                ... must contain exactly 64 colors
            ]
        },
        {
            "ID": "anotherCustomRGB",
            "Colors": [
                "#FFFFFF",
                ... must contain exactly 64 colors
            ]
        }
    ]

#### Attributes
* ID

  Required, string

  The ID of the table is used in the config when exporting bitmaps.

* Colors
  
  Required, array of strings

  Must contain exactly 64 entries. Each string can be any color format which can be parsed by SkiaSharp.

### NSSFiles
Required, must contain at least one entry

Each entry in the NSSFiles array will contain all the operations to be performed on one Nexxt save file. They are processed in the order specified in the config.

    {
        "SourceFile": "project2.nss",
        "ExportCHR": [ ],
        "ExportPalette": [ ],
        "ExportNametable": [ ],
        "ExportNametableAttributes": [ ],
        "ExportBitmap": [ ],
        "ExportNametableBitmap": [ ]
    }

### Attributes
* SourceFile

  Required, string

  The NSS file to load. Can be relative or fully qualified.

* ExportCHR, ExportPalette, ExportNametable, ExportNametableAttributes, ExportBitmap
 
  Optional, array of objects

  Every object in each of these arrays represents a single export operation using data from the parent NSS file. See below for details on each kind of operation.

### ExportCHR
Optional

Extracts some or all of the CHR data in the NSS file and saves it to a binary file.

    {
        "TargetFile": "bank00.chr",
        "Start": "$100",
        "Size": "$100"
    }

#### Attributes
* TargetFile

  Required, string
  
  File to which to save the CHR data.
* Start
  
  Optional, numeric string, defaults to "0"

  The index from which to start reading CHR data to save. NSS files seem to usually contain 16,384 (16K) bytes of CHR data. Each 8x8 pattern takes 16 bytes of CHR data for a total of 1024 tiles of which 256 tiles are shown at a time in Nexxt.
* Size

  Optional, numeric string, defaults to "1024"

  Number of bytes of data to save to TargetFile.

### ExportPalette
Optional

Extracts a single 16 byte of palette data from the NSS file and formats it as source code.

    {
        "TargetFile": "title_screen_palette.inc",
        "TargetSegmentName": "TitleScreenData",
        "TargetVariableName": "title_palette",
        "TargetAppend": false,
        "SourceSubPalette": 2
    }

The output to the target file for the above config is formatted as follows:

    .segment "TitleScreenData"
    title_palette:
    .byte $19,$21,$0F,$30
    .byte $19,$00,$10,$30
    .byte $19,$37,$21,$2C
    .byte $19,$27,$09,$29

#### Attributes

* TargetFile

  Required, string
  
  The text file to which to save the formatted palette data. If the Append attribute is true then any existing file is not overwritten and instead the data is appended to the end of it. If Append is false (the default) any existing file is overwritten.

* TargetSegmentName

  Optional, string, defaults to null

  If set, the string `.segment "TargetSegmentName"` will be written to the start of the output.

* TargetVariableName

  Optional, string, defaults to null

  If set, the string `TargetVariableName:` will be written to the output after the TargetSegmentName (if any) and before the palette data.

* TargetAppend

  Optional, bool, defaults to false

  If false, TargetFile will be created and any existing file will be overwritten. If true and TargetFile already exists then the palette data will be appended to the end of the existing file.

* SourceSubPalette

  Optional, number, defaults to 0

  Each NSS file stores 4 palettes (labeled A-D in the Nexxt app). This attribute selects which one to write (0 = A, 1 = B etc...).

### ExportNametable
Optional

Writes the full 960 byte nametable from the NSS file to a binary file.

    {
        "TargetFile": "title_screen_nametable.dat"
    }

#### Attributes
* TargetFile

  Required, string

  The file to which to save the 960 bytes of nametable data.

### ExportNametableAttributes
Optional

Writes the full 64 bytes of nametable attribute data from the NSS file to a binary file.

    {
        "TargetFile": "title_screen_nametable_attributes.dat"
    }

#### Attributes
* TargetFile
  
  Required, string

  The file to which to save the 64 bytes of nametable attribute data.

### ExportBitmap
Optional

Combines a subset of CHR data and palette data from the NSS file to render a PNG bitmap of those tiles. A lookup table is used to assign RGB values to the NES palette indices. There is a built in default lookup table which is identical to that in Nexxt but alternates can be provided via RGBLookupTables (see above).

    {
        "TargetFile": "tiles.png",
        "StartTileIndex": "$100",
        "TileCount": "$40",
        "Layout": 1,
        "RGBLookupID": "Default",
        "PaletteSetIndex": "2",
        "PaletteIndex": "3"
    }

#### Attributes
* TargetFile
  
  Required, string
  
  The file to which a PNG bitmap will be written.

* StartTileIndex

  Optional, numeric string, defaults to "0"

  Each NSS file contains 1024 tiles (16,384 bytes) of CHR tile data. This is shown in the Nexxt UI as 4 sets of 256 tiles each. StartTileIndex, if set, is the first tile from which to start drawing to the output bitmap. EG: A value of "$100" indicates the 256th tile which is the first tile of the second set of CHR data as shown in Nexxt.

* TileCount

  Optional, numeric string, defaults to "256"

  How many tiles, starting at StartTileIndex, to draw to the output bitmap.

* Layout
  
  Optional, number, defaults to 0

  Which layout from the following enum to use.

      public enum BitmapLayout { 
        Linear,   // Draws all tiles in a single row from left to right
        Rect,     // Draws tiles in rows 16 tiles wide
        Rect8By16 // Draws tiles in rows 16 tiles wide and set up for the NES 8x16 sprite mode (see below)
      }

  In Rect8By16 mode the tiles are arranged as follows:

      00 02 04 06 08 10 12 14 16 18 20 22 24 26 28 30
      01 03 05 07 09 11 13 15 17 19 21 23 25 27 29 31
      32 34 36 38 40 42 44 46 48 50 52 54 56 58 60 62
      33 35 37 39 41 43 45 47 49 51 53 55 57 59 61 63
      etc...

* RGBLookupID

  Optional, string, defaults to "Default"

  If set, the RGBLookupTable with the specified ID will be used to determine RGB color values for each NES color index.

* PaletteSetIndex

  Optional, numeric string, defaults to "0"

  In each NSS file there are 4 sets of 4 palettes each. PaletteSetIndex determines which set of 4 to use for this bitmap. Must be in the range 0-4.

* PaletteIndex

  Optional, numeric string, defaults to "0"

  Which palette in the specified palette set to use. Must be in the range 0-4.

### ExportNametableBitmap
Optional

Draws a PNG bitmap of the nametable shown in Nexxt. This combines nametable, nametable attributes, CHR patterns, palettes and an RGB lookup table (see above).

    {
        "TargetFile": "tiles.png",
        "RGBLookupID": "Default",
        "CHRIndex": "0",
        "PaletteSetIndex": "0"
    }

### Attributes
* TargetFile

  Required, string

  The file to which a PNG bitmap will be written.

* RGBLookupID

  Optional, string, defaults to "Default"

  If set, the RGBLookupTable with the specified ID will be used to determine RGB color values for each NES color index.

* CHRIndex

  Optional, numeric string, defaults to "0"

  In each NSS file there are 4 sets of CHR pattern data of 256 tiles each. CHRIndex determines which set of 256 tiles to use. Must be in the range 0-3.

* PaletteSetIndex

  Optional, numeric string, defaults to "0"

  In each NSS file there are 4 sets of 4 palettes each. PaletteSetIndex determines which set of 4 to use for this bitmap. Must be in the range 0-3.

## Example config.json

    {
      "LogConfig": {
        "Echo": true,
        "LogFile": "./bin/nexxtporter.log"
      },
      "NSSFiles": [
        {
          "SourceFile": "./nss/font.nss",
          "ExportCHR": [
            {
              "TargetFile": "./chr/font.chr",
              "Start": "0",
              "Size": "$400"
            }
          ]
        },
        {
          "SourceFile": "./nss/title.nss",
          "ExportCHR": [
            {
              "TargetFile": "./chr/title_a.chr",
              "Start": "0",
              "Size": "$0400"
            },
            {
              "TargetFile": "./chr/title_b.chr",
              "Start": "$0400",
              "Size": "$0400"
            },
            {
              "TargetFile": "./chr/title_c.chr",
              "Start": "$0800",
              "Size": "$0400"
            },
            {
              "TargetFile": "./chr/title_d.chr",
              "Start": "$0C00",
              "Size": "$0400"
            },
            {
              "TargetFile": "./chr/title_e.chr",
              "Start": "$1000",
              "Size": "$0400"
            },
            {
              "TargetFile": "./chr/title_f.chr",
              "Start": "$1400",
              "Size": "$0400"
            },
            {
              "TargetFile": "./chr/title_g.chr",
              "Start": "$1800",
              "Size": "$0400"
            },
            {
              "TargetFile": "./chr/title_h.chr",
              "Start": "$1C00",
              "Size": "$0400"
            }
          ],
          "ExportNametable": [
            {
              "TargetFile": "./nam/title_nametable.dat"
            }
          ],
          "ExportNametableAttributes": [
            {
              "TargetFile": "./nam/title_nametable_attributes.dat"
            }
          ],
          "ExportPalette": [
            {
              "TargetFile": "./src/title_palette.inc",
              "TargetSegmentName": "TITLE_PRG",
              "TargetVariableName": "title_palette_a",
              "TargetAppend": false,
              "SourceSubPalette": 0
            },
            {
              "TargetFile": "./src/title_palette.inc",
              "TargetSegmentName": "TITLE_PRG",
              "TargetVariableName": "title_palette_b",
              "TargetAppend": true,
              "SourceSubPalette": 1
            },
            {
              "TargetFile": "./src/title_palette.inc",
              "TargetSegmentName": "TITLE_PRG",
              "TargetVariableName": "title_palette_c",
              "TargetAppend": true,
              "SourceSubPalette": 2
            },
            {
              "TargetFile": "./src/title_palette.inc",
              "TargetSegmentName": "TITLE_PRG",
              "TargetVariableName": "title_palette_d",
              "TargetAppend": true,
              "SourceSubPalette": 3
            }
          ],
          "ExportBitmap": [
            {
              "TargetFile": "./bin/font.png",
              "StartTileIndex": "0",
              "TileCount": "64",
              "Layout": 1,
              "PaletteSetIndex": "0",
              "PaletteIndex": "0"
            }
          ]
        }
      ]
    }