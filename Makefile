.PHONY: build clean publish run

build:
	dotnet build

clean:
	dotnet clean

publish:
	dotnet publish DIMS.Desktop -r linux-x64 -o publish -c Release --sc

run:
	dotnet run --project DIMS.Desktop

