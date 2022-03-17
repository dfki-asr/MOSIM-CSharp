# MOSIM - CSharp

This repository contains several Visual Studio projects for MOSIM written in C#. The core implementation and compiled thrift interface are included from [MMICSharp-Core](https://github.com/dfki-asr/MMICSharp-Core) as a submodule. To change the thrift interface, please look at the [MOSIM main repository](https://github.com/dfki-asr/MOSIM). 

For more documentation on the MOSIM Framework and its development, please visit the [MOSIM main repository](https://github.com/dfki-asr/MOSIM). 

## Usage

To clone this repository including the submodule, please utilize the following command in a suitable shell
```Console
git clone --recurse-submodules --remote-submodules git@github.com:dfki-asr/MOSIM-CSharp.git
```
or ensure in your git tool, that submodules are recursed. 

## Repository Structure
- **Core**: Containing Visual Studio projects to compile the MMICSharp-Core, the Launcher, Adapter and CoSimulator. 
- **MMUs**: Containing open-source MMUs written in C#
- **Services**: Containing open-source services written in C#
- **Tools**: Containing open-source tools written in C# 

## Contributing

If you would like to contribute code you can do so through GitHub by forking the repository and sending a pull request.

If you want to get involved more actively in the MOSIM project, please contact Janis Sprenger (DFKI) for further information.

## License

This project is licensed under the [MIT License](./LICENSE). 

Notice: Before you use the program in productive use, please take all necessary precautions, e.g. testing and verifying the program with regard to your specific use. The program was tested solely for our own use cases, which might differ from yours.

## Authors

As this project was merged from different git repositories, this list contains all additional authors, not tracked properly by github. 

- Felix Gaisbauer (Daimler)
- Andreas Kaiser (Daimler)
- Janis Sprenger (DFKI)
- Adam Klodowski (LUT)
