{
  description = "Development environment for Movie Tickets .NET project";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-24.05";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = {
    self,
    nixpkgs,
    flake-utils,
  }:
    flake-utils.lib.eachDefaultSystem (system: let
      pkgs = import nixpkgs {inherit system;};
      dotnet-sdk = pkgs.dotnet-sdk_8;
    in {
      devShells.default = pkgs.mkShell {
        packages = with pkgs; [
          dotnet-sdk # yes
          omnisharp-roslyn # lsp
          git # might as well, its for a devshell lol.
          sqlite
          icu
          fontconfig
          freetype
          harfbuzz
          graphite2
          xorg.libX11
          xorg.libXcursor
          xorg.libXrender
          xorg.libXi
          xorg.libXrandr
          xorg.libICE
          xorg.libSM
          wineWowPackages.staging # Imagine using WinForms, smh.
          winetricks
        ];

        shellHook = ''
          export DOTNET_ROOT=${dotnet-sdk}
          export PATH=$HOME/.dotnet/tools:$PATH
          export LD_LIBRARY_PATH=${pkgs.lib.makeLibraryPath [
            pkgs.fontconfig
            pkgs.freetype
            pkgs.harfbuzz
            pkgs.graphite2
            pkgs.zlib
            pkgs.libpng
            pkgs.libuuid
            pkgs.xorg.libX11
            pkgs.xorg.libXcursor
            pkgs.xorg.libXrender
            pkgs.xorg.libXi
            pkgs.xorg.libXrandr
            pkgs.xorg.libICE
            pkgs.xorg.libSM
          ]}

          if ! command -v dotnet-ef >/dev/null 2>&1; then
            echo "dotnet-ef not found. Installing it globally via dotnet tool..."
            dotnet tool install --global dotnet-ef # tbh idm this but holy might as well let it run like that and install on its own...
          fi
        '';
      };
    });
}
