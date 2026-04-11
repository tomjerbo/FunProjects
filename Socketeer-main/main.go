package main

import (
	"encoding/json"
	"fmt"
	"log"
	"net"
	"time"

	"github.com/joho/godotenv"
	auth "socketeer.github.com/internal/auth"
)

const (
	HOST   = "192.168.1.147"
	PORT   = "8080"
	METHOD = "tcp"
)

type Connection struct {
	user       auth.User
	connection net.Conn
}

var connections map[string]Connection

func main() {
	godotenv.Load()

	connections = make(map[string]Connection)

	listener, err := net.Listen(METHOD, fmt.Sprintf("%s:%s", HOST, PORT))
	if err != nil {
		log.Fatal(err)
		return
	}
	defer listener.Close()

	fmt.Printf("Listening to port %s.\n", PORT)

	for {
		conn, err := listener.Accept()
		if err != nil {
			fmt.Print("TCP accept failed.\n")
			return
		}
		go handleConnection(conn)
	}
}

type Packet struct {
	Data []byte `json:"data"`
	Key  string `json:"key"`
}

func handleConnection(conn net.Conn) {
	for {
		buffer := make([]byte, 4096)
		n, err := conn.Read(buffer)
		if err != nil {
			closeConnection(conn, fmt.Sprintf("Failed to connect %s\n", conn.RemoteAddr()))
			break
		}

		var packet Packet
		if err := json.Unmarshal(buffer[:n], &packet); err != nil {
			closeConnection(conn, "Failed to parse message to json.")
			return
		}
		address := conn.RemoteAddr().String()
		fmt.Printf("Distributing ip address %s\n", address)
		connection, ok := connections[address]
		if !ok {
			fmt.Printf("Create user at %s\n", address)
			connections[address] = Connection{
				user: auth.User{
					IpAddress: address,
					IsAuthed:  false,
				},
				connection: conn,
			}
			connection = connections[address]
		}

//		if !connection.user.IsAuthed {
//			_, err := connection.user.ValidateApiKey(packet.Key)
//			if err != nil {
//				closeConnection(conn, fmt.Sprintf("Failed to authenticate user at %s\n", address))
//				return
//			}
//			connection.user.IsAuthed = true
//		}
		connection.user.IsAuthed = true

		time := time.Now().Format(time.ANSIC)
		responseStr := fmt.Sprintf("Valid client message recieved at %v", time)
		_, err = conn.Write([]byte(responseStr))

		go distributeMessage(&connection, packet)
	}
}

func distributeMessage(distConn *Connection, packet Packet) {
	for _, conn := range connections {
		fmt.Printf("Packet -> %v\n", string(packet.Data))
		if conn.user.IpAddress == distConn.user.IpAddress {
			continue
		}

		conn.connection.Write(packet.Data)
	}
}

func closeConnection(conn net.Conn, msg string) {
	if conn == nil {
		return
	}

	fmt.Printf("Closed connection -> %s\n", msg)

	go conn.Write([]byte(msg))

	conn.Close()
}
