
window.CMSEMantenimiento = {
    iniciarTabla: function (opciones) {
        const tabla = document.getElementById(opciones.tablaId);
        const cuerpo = tabla?.querySelector("tbody");
        const encabezados = tabla?.querySelectorAll("thead th") ?? [];
        const buscador = document.getElementById(opciones.buscadorId);
        const boton = document.getElementById(opciones.botonId);
        const mensajeSinResultados = document.getElementById(opciones.mensajeId);

        if (!tabla || !cuerpo) {
            console.error("CMSEMantenimiento: no se encontró la tabla o su cuerpo.", opciones.tablaId);
            return null;
        }

        let filas = Array.from(cuerpo.querySelectorAll("tr"));
        let elementoSeleccionado = null;
        let columnaOrdenada = -1;
        let direccionAscendente = true;

        function seleccionarFila(fila) {
            filas.forEach(otraFila => {
                otraFila.classList.remove("cmse-fila-seleccionada");
            });

            fila.classList.add("cmse-fila-seleccionada");
            elementoSeleccionado = fila.dataset.id;

            if (boton) {
                boton.disabled = false;
            }

            opciones.onSelect?.(elementoSeleccionado, fila);
        }

        function limpiarSeleccion() {
            filas.forEach(fila => {
                fila.classList.remove("cmse-fila-seleccionada");
            });

            elementoSeleccionado = null;

            if (boton) {
                boton.disabled = true;
            }

            opciones.onClear?.();
        }

        function aplicarBusqueda() {
            const texto = buscador?.value.toLowerCase().trim() ?? "";
            let visibles = 0;

            filas.forEach(fila => {
                const coincide = fila.textContent.toLowerCase().includes(texto);
                fila.style.display = coincide ? "" : "none";

                if (coincide) {
                    visibles++;
                } else if (fila.dataset.id === elementoSeleccionado) {
                    limpiarSeleccion();
                }
            });

            if (mensajeSinResultados) {
                mensajeSinResultados.style.display = visibles === 0 ? "block" : "none";
            }
        }

        function obtenerValor(fila, indice) {
            const celda = fila.cells[indice];

            return celda?.dataset.valor !== undefined
                ? celda.dataset.valor
                : (celda?.textContent.trim() ?? "");
        }

        function compararValores(valorA, valorB, tipo) {
            if (tipo === "numero") {
                const numeroA = valorA === "" ? null : Number(valorA);
                const numeroB = valorB === "" ? null : Number(valorB);

                if (numeroA === null) return numeroB === null ? 0 : 1;
                if (numeroB === null) return -1;

                return numeroA - numeroB;
            }

            if (tipo === "fecha") {
                if (!valorA) return valorB ? 1 : 0;
                if (!valorB) return -1;

                return valorA.localeCompare(valorB);
            }

            return valorA.localeCompare(valorB, "es", {
                sensitivity: "base",
                numeric: true
            });
        }

        filas.forEach(fila => {
            fila.addEventListener("click", function () {
                seleccionarFila(this);
            });

            if (opciones.onDoubleClick) {
                fila.addEventListener("dblclick", function () {
                    seleccionarFila(this);
                    opciones.onDoubleClick(elementoSeleccionado, this);
                });
            }
        });

        buscador?.addEventListener("input", aplicarBusqueda);

        if (opciones.ordenar) {
            encabezados.forEach((encabezado, indice) => {
                encabezado.style.cursor = "pointer";
                encabezado.title = "Haz clic para ordenar esta columna";

                encabezado.addEventListener("click", function () {
                    if (columnaOrdenada === indice) {
                        direccionAscendente = !direccionAscendente;
                    } else {
                        columnaOrdenada = indice;
                        direccionAscendente = true;
                    }

                    const tipo = this.dataset.tipo || "texto";

                    filas.sort((a, b) => {
                        const comparacion = compararValores(
                            obtenerValor(a, indice),
                            obtenerValor(b, indice),
                            tipo
                        );

                        return direccionAscendente ? comparacion : -comparacion;
                    });

                    filas.forEach(fila => cuerpo.appendChild(fila));

                    encabezados.forEach(th => {
                        th.removeAttribute("aria-sort");
                        th.textContent = th.textContent.replace(/ [▲▼]$/, "");
                    });

                    this.setAttribute(
                        "aria-sort",
                        direccionAscendente ? "ascending" : "descending"
                    );

                    this.textContent += direccionAscendente ? " ▲" : " ▼";

                    aplicarBusqueda();
                });
            });
        }

        boton?.addEventListener("click", function () {
            if (elementoSeleccionado) {
                opciones.onButton?.(elementoSeleccionado);
            }
        });

        if (opciones.atajos) {
            document.addEventListener("keydown", function (event) {
                if (event.ctrlKey || event.altKey || event.metaKey || event.repeat) {
                    return;
                }

                const elemento = event.target;

                if (
                    elemento instanceof HTMLElement &&
                    (elemento.tagName === "INPUT" ||
                        elemento.tagName === "TEXTAREA" ||
                        elemento.tagName === "SELECT" ||
                        elemento.isContentEditable)
                ) {
                    return;
                }

                const accion = opciones.atajos[event.key.toLowerCase()];

                if (accion) {
                    const resultado = accion(elementoSeleccionado, event);

                    if (resultado !== false) {
                        event.preventDefault();
                    }
                }
            });
        }

        aplicarBusqueda();

        return {
            obtenerSeleccionado: () => elementoSeleccionado,
            seleccionar: seleccionarFila,
            limpiarSeleccion: limpiarSeleccion,
            aplicarBusqueda: aplicarBusqueda
        };
    }
};
