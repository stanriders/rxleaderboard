/**
 * This file was auto-generated by openapi-typescript.
 * Do not make direct changes to the file.
 */

export interface paths {
    "/scores": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["Score"][];
                        "application/json": components["schemas"]["Score"][];
                        "text/json": components["schemas"]["Score"][];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/scores/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    id: number;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["Score"];
                        "application/json": components["schemas"]["Score"];
                        "text/json": components["schemas"]["Score"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/players": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: {
                    page?: number;
                    search?: string;
                };
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["PlayersResult"];
                        "application/json": components["schemas"]["PlayersResult"];
                        "text/json": components["schemas"]["PlayersResult"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/players/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    id: number;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["PlayersDataResponse"];
                        "application/json": components["schemas"]["PlayersDataResponse"];
                        "text/json": components["schemas"]["PlayersDataResponse"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/players/{id}/scores": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    id: number;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["Score"][];
                        "application/json": components["schemas"]["Score"][];
                        "text/json": components["schemas"]["Score"][];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/beatmaps": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: {
                    page?: number;
                };
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["Beatmap"][];
                        "application/json": components["schemas"]["Beatmap"][];
                        "text/json": components["schemas"]["Beatmap"][];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/beatmaps/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    id: number;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["Beatmap"];
                        "application/json": components["schemas"]["Beatmap"];
                        "text/json": components["schemas"]["Beatmap"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/beatmaps/{id}/scores": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    id: number;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["Score"][];
                        "application/json": components["schemas"]["Score"][];
                        "text/json": components["schemas"]["Score"][];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/scores/add": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: {
            parameters: {
                query?: {
                    id?: number;
                };
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["Score"];
                        "application/json": components["schemas"]["Score"];
                        "text/json": components["schemas"]["Score"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/stats": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/plain": components["schemas"]["StatsResponse"];
                        "application/json": components["schemas"]["StatsResponse"];
                        "text/json": components["schemas"]["StatsResponse"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
}
export type webhooks = Record<string, never>;
export interface components {
    schemas: {
        Beatmap: {
            /** Format: int32 */
            id: number;
            artist: string | null;
            title: string | null;
            /** Format: int32 */
            creatorId: number;
            /** Format: int32 */
            beatmapSetId: number;
            difficultyName: string | null;
            /** Format: double */
            approachRate: number;
            /** Format: double */
            overallDifficulty: number;
            /** Format: double */
            circleSize: number;
            /** Format: double */
            healthDrain: number;
            /** Format: double */
            beatsPerMinute: number;
            /** Format: int32 */
            circles: number;
            /** Format: int32 */
            sliders: number;
            /** Format: int32 */
            spinners: number;
            /** Format: double */
            starRatingNormal: number;
            /** Format: double */
            starRating?: number | null;
            /** Format: date-time */
            scoresUpdatedOn: string;
            status: components["schemas"]["BeatmapStatus"];
            /** Format: int32 */
            maxCombo: number;
        };
        /**
         * Format: int32
         * @enum {integer}
         */
        BeatmapStatus: 0 | 1 | 2 | 3 | 4 | 5 | 6;
        /**
         * Format: int32
         * @enum {integer}
         */
        Grade: 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8;
        PlayersDataResponse: {
            /** Format: int32 */
            id: number;
            countryCode: string | null;
            username: string | null;
            /** Format: double */
            totalPp?: number | null;
            /** Format: double */
            totalAccuracy?: number | null;
            /** Format: date-time */
            updatedAt: string | null;
            /** Format: int32 */
            rank: number | null;
        };
        PlayersResult: {
            players?: components["schemas"]["User"][] | null;
            /** Format: int32 */
            total?: number;
        };
        Score: {
            /** Format: int64 */
            id: number;
            /** Format: int32 */
            userId?: number;
            user?: components["schemas"]["User"];
            /** Format: int32 */
            beatmapId: number;
            beatmap?: components["schemas"]["Beatmap"];
            grade: components["schemas"]["Grade"];
            /** Format: double */
            accuracy: number;
            /** Format: int32 */
            combo: number;
            mods: string[] | null;
            /** Format: date-time */
            date: string;
            /** Format: int32 */
            totalScore: number;
            /** Format: int32 */
            count50: number;
            /** Format: int32 */
            count100: number;
            /** Format: int32 */
            count300: number;
            /** Format: int32 */
            countMiss: number;
            /** Format: int32 */
            spinnerBonus: number | null;
            /** Format: int32 */
            spinnerSpins: number | null;
            /** Format: int32 */
            legacySliderEnds: number | null;
            /** Format: int32 */
            sliderTicks: number | null;
            /** Format: int32 */
            sliderEnds: number | null;
            /** Format: int32 */
            legacySliderEndMisses: number | null;
            /** Format: int32 */
            sliderTickMisses: number | null;
            /** Format: double */
            pp?: number | null;
            isBest: boolean;
        };
        StatsResponse: {
            /** Format: int32 */
            scoresTotal?: number;
            /** Format: int32 */
            usersTotal?: number;
            /** Format: int32 */
            beatmapsTotal?: number;
            /** Format: int32 */
            beatmapsToUpdate?: number;
            /** Format: double */
            updateRunLengthEstimate?: number;
        };
        User: {
            /** Format: int32 */
            id: number;
            countryCode: string | null;
            username: string | null;
            /** Format: double */
            totalPp?: number | null;
            /** Format: double */
            totalAccuracy?: number | null;
            /** Format: date-time */
            updatedAt: string | null;
        };
    };
    responses: never;
    parameters: never;
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export type operations = Record<string, never>;
