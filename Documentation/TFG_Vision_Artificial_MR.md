# TFG: Implementación de técnicas de visión artificial para detección de objetos en realidad mixta

## Descripción del proyecto
Proyecto orientado a evaluar y comparar técnicas de visión artificial y estimación de pose aplicadas a vídeo estereoscópico en entornos de realidad mixta, usando **Meta Quest 3**. Se comparan algoritmos clásicos de visión por computador frente a los sistemas de tracking nativos del visor (pose del casco y mandos), analizando precisión espacial y temporal y coste computacional.

## Tecnologías y entorno

- **Hardware:** Meta Quest 3
- **Software:** Unity + VSCode
- **Lenguajes:** C# (Unity), Python
- **Librerías:** OpenCV, NumPy
- **Repositorio base:** Unity-PassthroughCameraApiSamples (Oculus)

## Pipeline experimental

1. **Escena controlada en Unity**
2. **Captura de vídeo estereoscópico** con Passthrough Camera API
3. **Exportación de datos a PC**
4. **Postprocesado en Python**
5. **Análisis y comparación de métricas**

## Pipeline de visión artificial

1. **Rectificación estéreo**
2. **Cálculo de mapa de disparidad**
3. **Conversión a mapa de profundidad**
4. **Generación de nube de puntos**
5. **Surface matching mediante ICP**
6. **Estimación de pose (R, t)**

## Datos a registrar por frame

- Imágenes por visor
- Intrínsecos de las cámaras
- Pose del casco
- Pose de los mandos
- Timestamps
- Almacenamiento en disco para análisis offline

## Métricas principales

- Error de reproyección
- Error punto a punto
- Error de pose (traslación y rotación)
- Consistencia temporal entre frames
- Completitud de la nube de puntos

## Condiciones experimentales

Escenarios con variación de iluminación, oclusiones, reflejos, texturas y carga computacional.

## Estructura del proyecto

- **UnityProject** (captura, escena, exportación de datos)
- **PythonProcessing** (preprocesado, profundidad, nube de puntos, ICP, métricas)
- **Documentation**

## Objetivo final

Identificar las técnicas más adecuadas para aplicaciones inmersivas, equilibrando precisión, eficiencia computacional y compatibilidad con hardware de consumo.
