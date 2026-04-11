package auth

import (
	"errors"
	"fmt"
	"os"
)

type User struct {
	IpAddress string `json:"ipAdress"`
	IsAuthed  bool   `json:"isAuthed"`
}

func (u *User) ValidateApiKey(apiKey string) (string, error) {
	ak := os.Getenv("APIKEY")
	fmt.Printf("In -> %s, env -> %s\n", apiKey, ak)
	if ak != apiKey {
		return "", errors.New("Failed to validate api key.")
	}

	return apiKey, nil
}
